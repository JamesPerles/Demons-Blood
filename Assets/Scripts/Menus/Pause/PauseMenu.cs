using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
public class PauseMenu : SubMenu
{
public static PauseMenu instance;
public TextMeshProUGUI walletText;
public KeyCode pauseKey = KeyCode.Escape;
public bool pauseTimeScale = true;
public bool isOpen {get; private set;} = false;
public TabGroup topTabGroup;
public TabGroup miniTabGroup;
public KeyCode nextTopTabKey = KeyCode.E;
public KeyCode previousTopTabKey = KeyCode.LeftAlt;
public GameObject rosterPanel;
public GameObject detailPanel;
public GameObject statsPanel;
public PartyMenu partyController;
public InventoryMenu inventoryController;
public QuestMenu questController;
public ForgeMenu forgeController;
public MiscellaneousMenu miscController;
public MapMenu mapController;
public KeyCode rosterSwapKey = KeyCode.Tab;
bool partyTabActive = false;
ICardHighlightHandler activeCardHandler;
IPageableTab activePageableTab;
void Awake()
    {
        if(instance == null) 
        {instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
     }
     else if(instance != this)
        {
            Destroy(transform.root.gameObject);
            return;
        }
    }
        void OnDestroy()
        {
            if(instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if(scene.name == "BattleScene" && isOpen) Close();
        }
     void Start()
    {
        SetDisplayActive(false);
     }
     protected override void Update()
    {
        if(BattleManager.instance != null) return;
        if(Input.GetKeyDown(pauseKey))
        {
            if(isOpen) Close();
            else Open(); 
            return;
        }
        if(!isOpen) return;
        if(Input.GetKeyDown(KeyCode.Backspace)) HandleBack();
        if(Input.GetKeyDown(KeyCode.Space)) NextPage();
        HandleTabInput();
    }
    void HandlePageInput()
    {
        if(activePageableTab != null) activePageableTab.NextPage();
        else NextPage();
    }
    public void Open()
    {
        isOpen = true;
       SetDisplayActive(true);
        if(pauseTimeScale) Time.timeScale = 0f;
        UpdateWallet();
        ClearScreenHistory();
        SetupTopTabs();
    } 
    public override void Close()
    {
        isOpen = false;
        SetDisplayActive(false);
        if(pauseTimeScale) Time.timeScale = 1f;
        ClearEntries();
        ClearScreenHistory();
        partyTabActive = false;
        activeCardHandler = null;
        if(partyController != null) partyController.ResetState();
    }
    void UpdateWallet()
    {
        if (walletText != null && WalletManager.instance != null)
        walletText.text = $"{WalletManager.instance.currentGold} Gold";
    }
public void ShowRosterPanel(bool showStatsExtra = false)
    {
        if(rosterPanel != null) rosterPanel.SetActive(true);
        if(detailPanel != null) detailPanel.SetActive(false);
        if(statsPanel != null) statsPanel.SetActive(showStatsExtra);
    }
    public void ShowDetailPanel()
    {
        if(rosterPanel != null) rosterPanel.SetActive(false);
        if(detailPanel != null) detailPanel.SetActive(true);
        if(statsPanel != null) statsPanel.SetActive(false);
    }
    public void ShowSplitPanel()
    {
        if(rosterPanel != null) rosterPanel.SetActive(false);
        if(detailPanel != null) detailPanel.SetActive(true);
        if(statsPanel != null) statsPanel.SetActive(false);
    }
    public void SetStatsPanelActive(bool active)
    {
        if(statsPanel != null) statsPanel.SetActive(active);
    }
    public void SetCardHighlightHandler(ICardHighlightHandler handler) => activeCardHandler = handler;
    public void ClearCardHighlightHandler() => activeCardHandler = null;
    public void SetPageableTab(IPageableTab tab) => activePageableTab = tab;
    public void ClearPageableTab() => activePageableTab = null;
    public override void EntryHighlight(GameObject entry)
    {
        base.EntryHighlight(entry);
        activeCardHandler?.OnCardHighlighted(entry);
    }
    public void PrepareTabSwitch()
    {
        ClearEntries();
        ClearScreenHistory();
        ClearCardHighlightHandler();
        ClearPageableTab();
        (partyController as ITabHider)?.HideVisuals();
        (questController as ITabHider)?.HideVisuals();
        (mapController as ITabHider)?.HideVisuals();
        (miscController as ITabHider)?.HideVisuals();
        (inventoryController as ITabHider)?.HideVisuals();
        (forgeController as ITabHider)?.HideVisuals();
    }
    void SetupTopTabs()
    {
        System.Collections.Generic.List<TabDefinition> tabs = new System.Collections.Generic.List<TabDefinition>
        {
        new TabDefinition("Party", () => {partyTabActive = true; if(partyController != null) partyController.OpenTab();}),
        new TabDefinition("Inventory", () => {partyTabActive = false; if(inventoryController != null) inventoryController.OpenTab();}),
        new TabDefinition("Quests", () => {partyTabActive = false; if(questController != null) questController.OpenTab();}),
        new TabDefinition("Forge", () => {partyTabActive = false; if(forgeController != null) forgeController.OpenTab();}),
        new TabDefinition("Misc", () => {partyTabActive = false; if(miscController != null) miscController.OpenTab();}),
        new TabDefinition("Map", () => {partyTabActive = false; if(mapController != null) mapController.OpenTab();}),
        };
        if(topTabGroup != null) topTabGroup.SetTabs(tabs,0);
    }
    void HandleBack()
    {
        if(partyTabActive && partyController != null)
        {
            partyController.HandleBack();
            return;
        }
        if(ScreenDepth > 1) PreviousScreen();
        else Close();
    }
    void HandleTabInput()
    {
        if(Input.GetKeyDown(nextTopTabKey))
        {
            if(partyTabActive && partyController != null && partyController.InCharacterDetail)
            partyController.ExitCharacterDetail();
            if(topTabGroup != null) topTabGroup.NextTab();
            return;
        }
        if(Input.GetKeyDown(previousTopTabKey))
        {
            if(partyTabActive && partyController != null && partyController.InCharacterDetail)
            partyController.ExitCharacterDetail();
            if(topTabGroup != null) topTabGroup.PreviousTab();
            return;
        }
        if(partyTabActive && partyController != null) partyController.HandleTabInput();
    }
}