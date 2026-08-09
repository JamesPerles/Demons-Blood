using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class MenuOption
{
    public string label;
    public System.Action onSelect;
    public System.Func<List<MenuOption>> getChildren;
    public bool enabled = true;
    public float? childFontSize;
    public int? childColumns;
    public Vector2? childCellSize;
    public Vector2? childSpacing;//still dont understand the ? purpose here./is their a better way to do this
    public Color? textColor;
    public MenuOption(string label, System.Action onSelect)
    {this.label = label; this.onSelect = onSelect;}
    public MenuOption(string label, System.Func<List<MenuOption>> getChildren)
    {this.label = label; this.getChildren = getChildren;}
    public MenuOption(string label, System.Func<List<MenuOption>> getChildren, 
    float childFontSize, int childColumns, Vector2 childCellSize, Vector2 childSpacing)
    {this.label = label; this.getChildren = getChildren; this.childFontSize = childFontSize; 
    this.childColumns = childColumns; this.childCellSize = childCellSize; this.childSpacing = childSpacing;}
    }
public class MenuScreen
{
    public List<MenuOption> allOptions;
    public float fontSize;
    public int columns;
    public Vector2 cellSize;
    public Vector2 spacing;
    public int currentPage;
    public MenuScreen(List<MenuOption> allOptions, float fontSize, int columns, Vector2 cellSize, Vector2 spacing)
    {
        this.allOptions = allOptions;
        this.fontSize = fontSize;
        this.columns = columns;
        this.cellSize = cellSize;
        this.spacing = spacing;
        this.currentPage = 0;
    }
}
public class CommandMenu : MonoBehaviour
{
    public static CommandMenu instance;
    public bool actionSelected = false;
    public bool undoRequested = false;
    public enum PlayerActionType { Attack, Defend, Item, Run, Art, Spell, Fusion, PartySwap, Transform}
    public PlayerActionType selectedAction;
    public Item selectedItem;
    public Learnable selectedLearnable;
    ActiveStats currentActiveStats;
    public ActiveStats selectedSwapTarget;
    public Transform optionsGrid;
    public GridLayoutGroup grid;
    public GameObject entryPrefab;
    public float mainFontSize = 36f;
    public int mainColumns = 2;
    public Vector2 mainCellSize = new Vector2(240f, 100f);
    public Vector2 mainSpacing = Vector2.zero;
    public float categoryFontSize = 36f;
    public int categoryColumns = 2;
    public Vector2 categoryCellSize = new Vector2(240f, 100f);
    public Vector2 categorySpacing = Vector2.zero;
    public float listFontSize = 24f;
    public int listColumns = 4;
    public Vector2 listCellSize = new Vector2(140f, 60f);
    public Vector2 listSpacing = Vector2.zero;
    public int entriesPerPage = 20;
    List<GameObject> spawnedEntries = new List<GameObject>();
    Stack<MenuScreen> screenHistory = new Stack<MenuScreen>();
    void Awake() 
    {
    if (instance == null) instance = this; else Destroy(gameObject);
    }
    void Start()
    {
        EnableMenu();
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Backspace))
        {
            if(screenHistory.Count > 0) PreviousPage();
            else if (!actionSelected) undoRequested = true;
        }
        if (Input.GetKeyDown(KeyCode.Space) && screenHistory.Count > 0) NextPage();
    }
  public void NextCharacter(ActiveStats character) //maybe add turn to name
    {
        currentActiveStats = character;
        ReturnToMain();
        EnableMenu();
        actionSelected = false;
        undoRequested = false;
    }
    public GameObject menuPanel;
    public void EnableMenu()
    {
        if(menuPanel != null) menuPanel.SetActive(true); 
        else gameObject.SetActive(true);
        EnableEntries(true);
        if (BattleTextBox.instance != null) BattleTextBox.instance.Clear();
    }
    public void DisableMenu()
    {
        EnableEntries(false);
    }
    public void HideMenu()
    {
        ClearSpawnedEntries();
        if(menuPanel != null) menuPanel.SetActive(false);
        else gameObject.SetActive(false);
    }
    public PlayerActionType GetSelectedAction() => selectedAction;
    List<MenuOption> MainMenu()
    {
        List<MenuOption> options = new List<MenuOption>
        {
           new MenuOption("Fight", FightMenu, categoryFontSize, categoryColumns, categoryCellSize, categorySpacing),
        new MenuOption("Defend", DefendSelected), new MenuOption("Item", ItemMenu), new MenuOption("Run", RunSelected) 
        };
        MenuOption swapOption = new MenuOption("Swap", PartySwapMenu, listFontSize, listColumns, listCellSize, listSpacing);
        swapOption.enabled = GetBenchMembers().Count > 0;
        options.Add(swapOption);
        MenuOption transformOption = new MenuOption("Transform", TransformSelected);
        transformOption.enabled = currentActiveStats.transformReady;
        options.Add(transformOption);
        return options;
    }
    List<ActiveStats> GetBenchMembers()
    {
        List<ActiveStats> bench = new List<ActiveStats>();
        if(PlayerParty.instance == null) return bench;
        List<ActiveStats> active = BattleManager.instance.GetActivePlayers();
        foreach (var member in PlayerParty.instance.playableCharacters)
        {
            ActiveStats stats = member.GetComponent<ActiveStats>();
            if(stats != null && !active.Contains(stats)) bench.Add(stats);
        }
        return bench;
    }
    List<MenuOption> PartySwapMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach(var member in GetBenchMembers())
        {
            ActiveStats captured = member;
            options.Add(new MenuOption(captured.playerStats.characterName, () =>
            {
                selectedAction = PlayerActionType.PartySwap;
                selectedSwapTarget = captured;
                FinalizeTurn();
            }));
        }
        return options;
    }
    void DefendSelected() 
    {
    selectedAction = PlayerActionType.Defend; FinalizeTurn();
    }
    void RunSelected()
    {
        selectedAction = PlayerActionType.Run; FinalizeTurn();
    }
    void TransformSelected()
    {
        selectedAction = PlayerActionType.Transform; FinalizeTurn();
    }
    List<MenuOption> FightMenu() => new List <MenuOption>
    {
        new MenuOption("Attack", () =>{selectedAction = PlayerActionType.Attack; FinalizeTurn();}), 
        new MenuOption("Arts", ArtMenu), new MenuOption("Spells", SpellMenu), new MenuOption("Fusions", FusionMenu)
    };
    List<MenuOption> ItemMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach (Item item in InventoryManager.Instance.items.OfType<Item>().Where(i => i.itemType == Item.ItemType.Consumable))
        {
            Item captured = item; //still wondering if the term captured is neccesary
            options.Add(new MenuOption(item.itemName, () =>
            {
                selectedAction = PlayerActionType.Item;
                selectedItem = captured; FinalizeTurn();
            }));
        } return options;
    }
    List<MenuOption> ArtMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach (var art in currentActiveStats.learnedArts)
        {
            Art captured = art;
            MenuOption option = new MenuOption(captured.artName, () => {selectedAction = PlayerActionType.Art; selectedLearnable = captured; FinalizeTurn();});
            option.enabled = currentActiveStats.currentHP >= captured.Cost;
            options.Add(option);
        } return options;
    }
    List<MenuOption> SpellMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach (var spell in currentActiveStats.learnedSpells)
        {
            Spell captured = spell;
            MenuOption option = new MenuOption (captured.spellName, () =>  {selectedAction = PlayerActionType.Spell; selectedLearnable = captured; FinalizeTurn();});
            option.enabled = currentActiveStats.currentMP >= captured.Cost;
            options.Add(option);
            } return options;
    }
    List <MenuOption> FusionMenu()
    {
        List<MenuOption> options = new List <MenuOption>();
        foreach (var fusion in currentActiveStats.learnedFusions)
        {
            Fusion captured = fusion;
            MenuOption option = new MenuOption(captured.fusionName, () => {selectedAction = PlayerActionType.Fusion; selectedLearnable = captured; FinalizeTurn();});
            option.enabled = currentActiveStats.currentHP >= captured.HPCost && currentActiveStats.currentMP >= captured.MPCost;
            options.Add(option);
        } return options;
    }
    void ShowMainMenu()
    {
    MenuScreen screen = new MenuScreen(MainMenu(), mainFontSize, mainColumns, mainCellSize, mainSpacing);
    FillMenu(screen);
    }
    void OpenScreen(List<MenuOption> options, float fontSize, int columns, Vector2 cellSize, Vector2 spacing)
    {
        MenuScreen screen = new MenuScreen(options, fontSize, columns, cellSize, spacing);
        screenHistory.Push(screen);
        FillMenu(screen);
    } 
    void FillMenu(MenuScreen screen)
    {
        ClearSpawnedEntries();
        ChangeGridSize(screen.columns, screen.cellSize, screen.spacing);
        if (screen.allOptions.Count == 0)
        {
            EmptyMenu(screen.fontSize); return;
        }
        int start = screen.currentPage * entriesPerPage;
        int count = Mathf.Min(entriesPerPage, screen.allOptions.Count - start);
        for (int i = start; i < start + count; i++)
        {
            MenuOption option = screen.allOptions[i];
            GameObject entry = Instantiate(entryPrefab, optionsGrid);
            var label = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = option.label;
                label.fontSize = screen.fontSize;
                if(option.textColor.HasValue) label.color = option.textColor.Value;
            }
            MenuOption captured = option;
            Button button = entry.GetComponent<Button>();
            button.interactable = option.enabled;
            button.onClick.AddListener(() => SelectCommand(captured));
            spawnedEntries.Add(entry);
        }
    } 
    void SelectCommand(MenuOption option)
    {
        if (option.getChildren != null)
        {
            float fontSize = option.childFontSize ?? listFontSize;
            int columns = option.childColumns ?? listColumns;
            Vector2 cellSize = option.childCellSize ?? listCellSize;
            Vector2 spacing = option.childSpacing ?? listSpacing;
             OpenScreen(option.getChildren(), fontSize, columns, cellSize, spacing);
        }
        else option.onSelect();
    }
    void NextPage()
    {
        MenuScreen screen = screenHistory.Peek();
        int maxPage = Mathf.Max(0, (screen.allOptions.Count - 1) / entriesPerPage);
        if (screen.currentPage >= maxPage) screen.currentPage = 0;
        else screen.currentPage = screen.currentPage + 1;
        FillMenu(screen);
    }
    void PreviousPage()
    {
        screenHistory.Pop();
        ClearSpawnedEntries();
        if(screenHistory.Count == 0) ShowMainMenu();
    else FillMenu(screenHistory.Peek());
    }
    void ChangeGridSize(int columns, Vector2 cellSize, Vector2 spacing)
    {
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.cellSize = cellSize;
        grid.spacing = spacing;
        RectTransform gridRect = grid.GetComponent<RectTransform>();
        float totalWidth = columns * cellSize.x + (columns - 1) * spacing.x + 
        grid.padding.left + grid.padding.right; gridRect.sizeDelta = new Vector2(totalWidth, gridRect.sizeDelta.y);
    } 
    void FinalizeTurn()
    {
      actionSelected = true;
      screenHistory.Clear();
      ShowMainMenu();
      DisableMenu();  
    }
    void ReturnToMain()
    {
       screenHistory.Clear();
       ShowMainMenu();
    }
    void EmptyMenu(float fontSize)
    {
        GameObject emptyEntry = Instantiate(entryPrefab, optionsGrid);
        var label = emptyEntry.GetComponentInChildren<TextMeshProUGUI>();
        if(label != null)
        {
            label.text = "Empty"; 
            label.fontSize = fontSize;
        }
        emptyEntry.GetComponent<Button>().interactable = false;
        spawnedEntries.Add(emptyEntry);
    }
    void ClearSpawnedEntries() 
    {
        foreach(var entry in spawnedEntries) Destroy(entry); 
        spawnedEntries.Clear();
        }
        void EnableEntries(bool interactable)
    {
        foreach (var entry in spawnedEntries)
        {
            var button = entry.GetComponent<Button>();
            if(button != null) button.interactable = interactable;
        }
    }
}

