using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
public class PauseMenu : SubMenu
{
public static PauseMenu instance;
public TextMeshProUGUI walletText;
public KeyCode pauseKey = KeyCode.Escape;
public bool pauseTimeScale = true;
public bool isOpen {get; private set;} = false;
public TabGroup topTabGroup;
public TabGroup miniTabGroup;
public TabGroup microTabGroup;
public KeyCode nextTopTabKey = KeyCode.E;
public KeyCode previousTopTabKey = KeyCode.LeftAlt;
public GameObject rosterPanel;
public GameObject listPanel;
public GameObject infoPanel;
public GameObject statsExtraPanel;
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
        if(instance == null) instance = this; else Destroy(gameObject);
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
    
    protected override string BreadcrumbPrefix() => "Pause";
    void UpdateWallet()
    {
        if (walletText != null && Wallet.instance != null)
        walletText.text = $"{Wallet.instance.currentGold} Gold";
    }
public void ShowRosterPanel()
    {
        if(rosterPanel != null) rosterPanel.SetActive(true);
        if(listPanel != null) listPanel.SetActive(false);
        if(infoPanel != null) infoPanel.SetActive(false);
        if(statsExtraPanel != null) statsExtraPanel.SetActive(false);
    }
    public void ShowListPanel()
    {
        if(rosterPanel != null) rosterPanel.SetActive(false);
        if(listPanel != null) listPanel.SetActive(true);
        if(infoPanel != null) infoPanel.SetActive(false);
        if(statsExtraPanel != null) statsExtraPanel.SetActive(false);
    }
    public void ShowInfoPanel()
    {
        if(rosterPanel != null) rosterPanel.SetActive(false);
        if(listPanel != null) listPanel.SetActive(false);
        if(infoPanel != null) infoPanel.SetActive(true);
        if(statsExtraPanel != null) statsExtraPanel.SetActive(false);
    }
    public void ShowSplitPanel()
    {
        if(rosterPanel != null) rosterPanel.SetActive(false);
        if(listPanel != null) listPanel.SetActive(true);
        if(infoPanel != null) infoPanel.SetActive(true);
        if(statsExtraPanel != null) statsExtraPanel.SetActive(false);
    }
    public void SetStatsExtraPanelActive(bool active)
    {
        if(statsExtraPanel != null) statsExtraPanel.SetActive(active);
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
        (questController as ITabVisualOwner)?.HideVisuals();
        (mapController as ITabVisualOwner)?.HideVisuals();
        (miscController as ITabVisualOwner)?.HideVisuals();
        (inventoryController as ITabVisualOwner)?.HideVisuals();
        (forgeController as ITabVisualOwner)?.HideVisuals();
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



