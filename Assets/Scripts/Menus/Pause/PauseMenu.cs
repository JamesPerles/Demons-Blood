using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text.RegularExpressions;
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
public KeyCode nextPageKey = KeyCode.PageDown;
public KeyCode previousPageKey = KeyCode.PageUp;
bool partyTabActive = false;
ICardHighlightHandler activeCardHandler;
IPageableTab activePageableTab;
GameObject lastSelected;
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
            bool blocked = (DialogueBox.instance != null && DialogueBox.instance.isOpen) || (ShopMenu.instance != null && ShopMenu.instance.isOpen);
            if(!blocked)
            {
            if(isOpen) Close();
            else Open();  
            } 
            return;
        }
        if(!isOpen) return;
        if(DialogueBox.instance != null && (DialogueBox.instance.isOpen || DialogueBox.instance.closed)) return;
        if(Input.GetKeyDown(KeyCode.Backspace)) HandleBack();
        if(Input.GetKeyDown(nextPageKey)) HandlePageInput(1);
        if(Input.GetKeyDown(previousPageKey)) HandlePageInput(-1);
        SyncHighlightToSelection();
        ClearHighlightWhenTabFocused();
        EnsureSelection();
        HandleEdgePageInput();
        HandleTabInput();
        lastSelected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
    }
    void HandlePageInput(int direction)
    {
        if(activePageableTab != null) 
        {
            if(direction > 0) activePageableTab.NextPage();
            else activePageableTab.PreviousPage();
        }
        else if(direction > 0) NextPage();
        else PreviousPage();
    }
    public void Open()
    {
        isOpen = true;
        lastSelected = null;
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
    enum NavZone {Other, Entry, MiniTab, TopTab }
    NavZone ZoneOf(GameObject selected)
    {
        if(selected == null) return NavZone.Other;
        if(miniTabGroup != null && miniTabGroup.OwnsButton(selected)) return NavZone.MiniTab;
        if(topTabGroup != null && topTabGroup.OwnsButton(selected)) return NavZone.TopTab;
        if(EntryForSelection(selected) != null) return NavZone.Entry;
        return NavZone.Other;
    }
    bool IsTabZone(NavZone zone)
    {
        return zone == NavZone.MiniTab || zone == NavZone.TopTab;
    }
    bool MiniTabsShowing()
    {
        return miniTabGroup != null && miniTabGroup.gameObject.activeInHierarchy
        && miniTabGroup.CurrentButton != null && miniTabGroup.CurrentButton.activeInHierarchy;
    }
    void SelectObject(GameObject target)
    {
        if(target == null || !target.activeInHierarchy || EventSystem.current == null) return;
        if(EventSystem.current.currentSelectedGameObject != target) EventSystem.current.SetSelectedGameObject(target);
    }
    void SelectZone(NavZone zone)
    {
        if(zone == NavZone.TopTab) SelectObject(topTabGroup != null ? topTabGroup.CurrentButton : null);
        else if(zone == NavZone.MiniTab) SelectObject(miniTabGroup != null ? miniTabGroup.CurrentButton : null);
        else if(zone == NavZone.Entry)
        {
            GameObject first = FirstSelectableEntry();
            if(first != null) FocusEntry(first);
        }
    }
    bool EnforceZoneRules(GameObject raw, int horizontal, int vertical, bool freshPress)
    {
        if(EventSystem.current == null || lastSelected == null || !lastSelected.activeInHierarchy) return false;
        NavZone previousZone = ZoneOf(lastSelected);
        NavZone currentZone = ZoneOf(raw);
        bool moved = raw != lastSelected;
        if(!moved && !freshPress) return false;
        bool isHorizontal = horizontal != 0;
        bool miniShowing = MiniTabsShowing();
        if(previousZone == NavZone.Entry)
        {
            if(!moved || !IsTabZone(currentZone)) return false;
            GameObject previousEntry = EntryForSelection(lastSelected);
            if(!isHorizontal && vertical > 0)
            {
                GameObject above = FindNeighborEntry(previousEntry, Vector2.up);
                if(above != null)
                {
                    FocusEntry(above);
                    return true;
                }
                NavZone wanted = miniShowing ? NavZone.MiniTab : NavZone.TopTab;
                if(currentZone != wanted) SelectZone(wanted);
                return true;
            }
            Vector2 direction = isHorizontal ? new Vector2(horizontal, 0f) : Vector2.down;
            GameObject next = FindNeighborEntry(previousEntry, direction);
            FocusEntry(next != null ? next : previousEntry);
            return true;
        }
        if(IsTabZone(previousZone))
        {
            if(isHorizontal)
            {
                TabGroup group = previousZone == NavZone.MiniTab ? miniTabGroup : topTabGroup;
                GameObject neighbor = group != null ? group.GetNeighborButton(lastSelected, horizontal) : null;
                SelectObject(neighbor != null ? neighbor : lastSelected);
                return true;
            }
            if(vertical > 0)
            {
                if(previousZone == NavZone.TopTab)
                {
                    if(!moved) return false;
                    SelectObject(lastSelected);
                    return true;
                }
                if(currentZone == NavZone.TopTab) return false;
                SelectZone(NavZone.TopTab);
                return true;
            }
            NavZone target = (previousZone == NavZone.TopTab && miniShowing) ? NavZone.MiniTab : NavZone.Entry;
            if(currentZone == target) return false;
            if(currentZone == NavZone.Other && target == NavZone.Entry) return false;
            SelectZone(target);
            return true;
        }
        if(moved && IsTabZone(currentZone))
        {
            if(!isHorizontal && vertical > 0)
            {
                NavZone wanted = miniShowing ? NavZone.MiniTab : NavZone.TopTab;
                if(currentZone != wanted) SelectZone(wanted);
                return true;
            }
            SelectObject(lastSelected);
            return true;
        }
        return false;
    }
    void HandleEdgePageInput()
    {
       int horizontalDown = Input.GetKeyDown(KeyCode.RightArrow) ? 1 : (Input.GetKeyDown(KeyCode.LeftArrow) ? -1 : 0);
       int verticalDown = Input.GetKeyDown(KeyCode.UpArrow) ? 1 : (Input.GetKeyDown(KeyCode.DownArrow) ? -1 : 0);
       int horizontalHeld = Input.GetKey(KeyCode.RightArrow) ? 1 : (Input.GetKey(KeyCode.LeftArrow) ? -1 : 0);
       int verticalHeld = Input.GetKey(KeyCode.UpArrow) ? 1 : (Input.GetKey(KeyCode.DownArrow) ? -1 : 0);
        bool freshPress = horizontalDown != 0 || verticalDown != 0;
        int horizontal = freshPress ? horizontalDown : horizontalHeld;
        int vertical = freshPress ? verticalDown : verticalHeld;
        if(horizontal == 0 && vertical == 0) return;
        GameObject raw = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        GameObject previousEntry = EntryForSelection(lastSelected);
        if(freshPress && horizontal != 0 && previousEntry != null && TryEdgePage(previousEntry, horizontal)) return;
        if(EnforceZoneRules(raw, horizontal, vertical, freshPress)) return;
        if(!freshPress) return;
        if(raw == null || raw != lastSelected) return;
        GameObject selected = EntryForSelection(raw);
        if(selected == null) return;
        Vector2 direction = horizontal != 0 ? new Vector2(horizontal, 0f) : new Vector2(0f, vertical);
        GameObject neighbor = FindNeighborEntry(selected, direction);
        if(neighbor != null)
        {
            FocusEntry(neighbor);
            return;
        }
        if(vertical > 0)
        {
            GameObject tab = VisibleTabButton();
            if(tab != null) EventSystem.current.SetSelectedGameObject(tab);
        }
    }
    bool TryEdgePage(GameObject source, int direction)
    {
        if(activePageableTab == null) return false;
        if(!TryGetPageInfo(out int pageNow, out int pageTotal)) return false;
        if(direction > 0 && pageNow >= pageTotal) return false;
        if(direction < 0 && pageNow <= 1) return false;
        if(partyTabActive && partyController != null && partyController.InCharacterDetail && IsRosterCard(source)) return false;
        List<List<GameObject>> rows = GetEntryRows();
        int rowIndex = rows.FindIndex(row => row.Contains(source));
        if(rowIndex < 0) return false;
        List<GameObject> currentRow = rows[rowIndex];
        bool atEdge = direction > 0 ? currentRow[currentRow.Count - 1] == source : currentRow[0] == source;
        if(!atEdge) return false;
        if(direction > 0) activePageableTab.NextPage();
        else activePageableTab.PreviousPage();
        if(spawnedEntries.Contains(source))
        {
            FocusEntry(source);
            return true;
        }
        rows = GetEntryRows();
        if(rows.Count == 0) return true;
        List<GameObject> targetRow = rows[Mathf.Min(rowIndex, rows.Count - 1)];
        FocusEntry(direction > 0 ? targetRow[0] : targetRow[targetRow.Count - 1]);
        return true;
    }
    bool IsRosterCard(GameObject entry)
    {
        if(partyController == null || partyController.partyController == null) return false;
        Transform slotParent = partyController.partyController.slotParent;
        return slotParent != null && entry.transform.IsChildOf(slotParent);
    }
    GameObject FindNeighborEntry(GameObject source, Vector2 direction)
    {
      RectTransform sourceRect = source.transform as RectTransform;
      if(sourceRect == null) return null;
      Vector2 origin = sourceRect.TransformPoint(sourceRect.rect.center);
      GameObject best = null;
      float bestScore = float.MaxValue;
      foreach(GameObject entry in spawnedEntries)
        {
            if(entry == null || entry == source || !entry.activeInHierarchy) continue;
            Selectable selectable = GetEntrySelectable(entry);
            if(selectable != null && !selectable.IsInteractable()) continue;
            RectTransform rect = entry.transform as RectTransform;
            if(rect == null) continue;
            Vector2 offset = (Vector2)rect.TransformPoint(rect.rect.center) - origin;
            float forward = Vector2.Dot(offset, direction);
            if(forward <= 1f) continue;
            float sideways = Mathf.Abs(offset.x * direction.y - offset.y * direction.x);
            float score = forward + sideways * 2f;
            if(score < bestScore)
            {
                bestScore = score;
                best = entry;
            }
        }  
        return best;
    }
    GameObject FirstSelectableEntry()
    {
        return spawnedEntries.Find(entry =>
        {
            if(entry == null || !entry.activeInHierarchy) return false;
            Selectable selectable = GetEntrySelectable(entry);
            return selectable == null || selectable.IsInteractable();
        });
    }
    bool IsTabButton(GameObject candidate)
    {
        return (topTabGroup != null && topTabGroup.OwnsButton(candidate)) || (miniTabGroup != null && miniTabGroup.OwnsButton(candidate));
    }
    GameObject VisibleTabButton()
    {
        if(miniTabGroup != null && miniTabGroup.gameObject.activeInHierarchy && miniTabGroup.CurrentButton != null) return miniTabGroup.CurrentButton;
        if(topTabGroup != null) return topTabGroup.CurrentButton;
        return null;
    }
    void ClearHighlightWhenTabFocused()
    {
        if(highlightedEntry == null || EventSystem.current == null) return;
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if(selected == null || !IsTabButton(selected)) return;
        highlightedEntry = null;
        activeCardHandler?.OnCardHighlighted(null);
    }
    void EnsureSelection()
    {
        if(EventSystem.current == null) return;
        GameObject current = EventSystem.current.currentSelectedGameObject;
        if(current != null && current.activeInHierarchy) return;
        bool arrowPressed = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)
        || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow);
        if(!arrowPressed) return;
       GameObject tab = VisibleTabButton();
       if(tab != null)
        {
            EventSystem.current.SetSelectedGameObject(tab);
            return;
        }
        GameObject first = FirstSelectableEntry();
        if(first != null) FocusEntry(first);
    }
    bool TryGetPageInfo(out int pageNow, out int pageTotal)
    {
        pageNow = 1;
        pageTotal = 1;
        if(pageText == null || string.IsNullOrEmpty(pageText.text)) return false;
        Match match = Regex.Match(pageText.text, @"(\d+)\s*/\s*(\d+)");
        if(!match.Success) return false;
        pageNow = int.Parse(match.Groups[1].Value);
        pageTotal = int.Parse(match.Groups[2].Value);
        return pageTotal > 1;
    }
    List<List<GameObject>> GetEntryRows()
    {
        List<List<GameObject>> rows = new List<List<GameObject>>();
        Dictionary<Transform, List<GameObject>> byParent = new Dictionary<Transform, List<GameObject>>();
        foreach(GameObject entry in spawnedEntries)
        {
            if(entry == null || !entry.activeInHierarchy) continue;
            Transform parent = entry.transform.parent;
            if(parent == null) continue;
            if(!byParent.TryGetValue(parent, out List<GameObject> row))
            {
                row = new List<GameObject>();
                byParent[parent] = row;
                rows.Add(row);
            }
            row.Add(entry);
        }
        return rows;
    }

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