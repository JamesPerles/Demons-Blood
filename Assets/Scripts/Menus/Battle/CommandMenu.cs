using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class CommandMenu : SubMenu
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
    public ICombatant selectedTarget;
    public Transform targetGrid;
    public Transform allyTargetGrid;
    public Transform enemyTargetGrid;
    public CanvasGroup leftPanelGroup;
    public float dimmedAlpha = 0.55f;
    public Vector2 targetCellSize = new Vector2(280f, 26f);
    public float targetFontSize = 14f;
    List<GameObject> spawnedTargets = new List<GameObject>();
    Dictionary<GameObject, MenuOption> targetOptionMap = new Dictionary<GameObject, MenuOption>();
    bool isTargeting = false;
    string targetingBreadcrumbSuffix = "";
    void Awake() 
    {
    if (instance == null) instance = this; else Destroy(gameObject);
    }
    void Start()
    {
        HideMenu();
    }
    protected override void Update()
    {
        if(Input.GetKeyDown(KeyCode.Backspace))
        {
            if(isTargeting) EndTargeting();
            else if(screenHistory.Count > 0) PreviousScreen();
            else if (!actionSelected) undoRequested = true;
        }
        if (Input.GetKeyDown(KeyCode.Space) && screenHistory.Count > 0) NextPage();
        if(Input.GetKeyDown(KeyCode.RightArrow)) EdgePage(1);
        if(Input.GetKeyDown(KeyCode.LeftArrow)) EdgePage(-1);   
    }
    void EdgePage(int direction)
    {
        if(isTargeting) return;
        if(screenHistory.Count == 0) return;
        MenuScreen screen = screenHistory.Peek();
        if(screen.columns <= 1) return;
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        int index = spawnedEntries.IndexOf(selected);
        if(index < 0) return;
        int col = index % screen.columns;
        if(direction > 0 && col == screen.columns - 1) NextPage();
        else if(direction < 0 && col == 0) PreviousPage();
    }
    public override void Close()
    {
        ShowMainMenu();
    }
  public void NextCharacter(ActiveStats character)
    {
        currentActiveStats = character;
        if(isTargeting) EndTargeting();
        selectedTarget = null;
        EnableMenu();
        ReturnToMain();
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
        ClearSpawnedTargets();
        ClearEntries();
        if(menuPanel != null) menuPanel.SetActive(false);
        else gameObject.SetActive(false);
    }
    public PlayerActionType GetSelectedAction() => selectedAction;
    List<MenuOption> MainMenu()
    {
        List<MenuOption> options = new List<MenuOption>
        {
        new MenuOption("Fight", FightMenu) {description = "Attack an enemy"},
        new MenuOption("Defend", DefendSelected) {description = "Reduce damage by defending against an enemy attack"}, 
        new MenuOption("Item", ItemMenu) {description = "Change equipment and use itmes in your inventory", childColumns = 3}, 
        new MenuOption("Run", RunSelected) {description = "Attempt to flee the battle"} 
        };
        MenuOption swapOption = new MenuOption("Swap", PartySwapMenu) {description = "Swap with a non active member of the party"};
        swapOption.enabled = GetBenchMembers().Count > 0;
        options.Add(swapOption);
        MenuOption transformOption = new MenuOption("Transform", TransformSelected) {description = "When the transform gauge is full unleash your full power"};
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
        new MenuOption("Attack",null) {description = "A basic attack", targetScope = MenuOption.TargetScope.Any, 
        onTargetSelect = (target) => {selectedAction = PlayerActionType.Attack; FinalizeTurn();}}, 
        new MenuOption("Arts", ArtMenu) {description = "Physical attacks that cost HP to use.", childColumns = 3},
         new MenuOption("Spells", SpellMenu) {description = "Magic attacks that cost MP to use", childColumns = 3}, 
         new MenuOption("Fusions", FusionMenu) {description = "A combination of Physical and Magical, costs both HP and MP to use", childColumns = 3}
    };
    List<MenuOption> ItemMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach (Item item in InventoryManager.instance.items.OfType<Item>().Where(i => i.itemType == Item.ItemType.Consumable))
        {
            Item captured = item;
            MenuOption option = new MenuOption(item.itemName, null);
            option.description = captured.description;
            option.targetScope = MenuOption.TargetScope.Any;
            option.onTargetSelect = (target) =>
            {
                selectedAction = PlayerActionType.Item;
                selectedItem = captured;
                FinalizeTurn();
            };
            options.Add(option);
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
            option.description = $"HP cost: {captured.Cost}";
            if(captured.isAOE)
            {
                option.onSelect = () => {selectedAction = PlayerActionType.Art; selectedLearnable = captured; FinalizeTurn();};
            }
            else
            {
                option.targetScope = MenuOption.TargetScope.Any;
                option.onTargetSelect = (target) => {selectedAction = PlayerActionType.Art; selectedLearnable = captured; FinalizeTurn();};
            }
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
            option.description = $"MP cost: {captured.Cost}";
            if(captured.isAOE)
            {
                option.onSelect = () => {selectedAction = PlayerActionType.Spell; selectedLearnable = captured; FinalizeTurn();};
            }
            else
            {
                 option.targetScope = MenuOption.TargetScope.Any;
                option.onTargetSelect = (target) => {selectedAction = PlayerActionType.Spell; selectedLearnable = captured; FinalizeTurn();};
            }
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
            option.description = $"HP cost: {captured.HPCost} MP cost: {captured.MPCost}";
            if(captured.isAOE)
            {
                option.onSelect = () => {selectedAction  = PlayerActionType.Fusion; selectedLearnable = captured; FinalizeTurn();};
            }
            else
            {
                option.targetScope = MenuOption.TargetScope.Any;
                option.onTargetSelect = (target) => {selectedAction = PlayerActionType.Fusion; selectedLearnable = captured; FinalizeTurn();};
            }
            options.Add(option);
        } return options;
    }
    void ShowMainMenu()
    {
    MenuScreen screen = new MenuScreen(MainMenu(), fontSize, 1, cellSize, spacing, "");
    FillMenu(screen);
    }
    void ReturnToMain()
    {
        screenHistory.Clear();
        ShowMainMenu();
    }
    protected override void SelectCommand(MenuOption option)
    {
     if(option.targetScope != MenuOption.TargetScope.None) {BeginTargeting(option); return;}
     base.SelectCommand(option);
    }
    public override void EntryHighlight(GameObject entry)
    {
        if(isTargeting)
        {
            if(targetOptionMap.ContainsKey(entry)) MoveCursor(entry);
            return;
        }
        base.EntryHighlight(entry);
    }
    protected override string BreadcrumbPrefix() => currentActiveStats != null ? currentActiveStats.playerStats.characterName : "";
    protected override string BreadcrumbSuffix() => isTargeting ? targetingBreadcrumbSuffix : "";
    void BeginTargeting(MenuOption sourceOption)
    {
        if(sourceOption.targetScope == MenuOption.TargetScope.Self)
        {
            selectedTarget = currentActiveStats;
            sourceOption.onTargetSelect(currentActiveStats);
            return;
        }
        List<ICombatant> allies = new List<ICombatant>();
        List<ICombatant> enemies = new List<ICombatant>();
        if(BattleManager.instance != null)
        {
            if(sourceOption.targetScope == MenuOption.TargetScope.Allies || sourceOption.targetScope == MenuOption.TargetScope.Any)
            allies.AddRange(BattleManager.instance.GetActivePlayers().Where(player => player.currentHP > 0).Cast<ICombatant>());
            if(sourceOption.targetScope == MenuOption.TargetScope.Enemies || sourceOption.targetScope == MenuOption.TargetScope.Any)
            enemies.AddRange(BattleManager.instance.GetLivingEnemies().Cast<ICombatant>());
        }
        List<MenuOption> allyOptions = new List<MenuOption>();
        foreach(ICombatant combatant in allies)
        {
            ICombatant captured = combatant;
            allyOptions.Add(new MenuOption(captured.currentName, () =>
            {
                selectedTarget = captured;
                EndTargeting();
                sourceOption.onTargetSelect(captured);
            }));
        }
        List<MenuOption> enemyOptions = new List<MenuOption>();
        foreach(ICombatant combatant in enemies)
        {
            ICombatant captured = combatant;
            enemyOptions.Add(new MenuOption(captured.currentName, () =>
            {
                selectedTarget = captured;
                EndTargeting();
                sourceOption.onTargetSelect(captured);
            }));
        }
        StartTargeting(allyOptions, enemyOptions, "Target");
    }
    void ApplyTargetGridSizing(Transform grid)
    {
        GridLayoutGroup layout = grid != null ? grid.GetComponent<GridLayoutGroup>() : null;
        if(layout == null) return;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 1;
        layout.cellSize = targetCellSize;
        layout.spacing =spacing;
    }
    void StartTargeting(List<MenuOption> allyOptions, List<MenuOption> enemyOptions, string breadcrumbSuffix)
    {
        ClearSpawnedTargets();
        if(allyOptions.Count == 0 && enemyOptions.Count == 0) return;
        isTargeting = true;
        targetingBreadcrumbSuffix = breadcrumbSuffix;
        if(leftPanelGroup != null) {leftPanelGroup.alpha = dimmedAlpha; leftPanelGroup.interactable = false;}
        if(targetGrid != null) targetGrid.gameObject.SetActive(true);
        if(detailText != null) detailText.gameObject.SetActive(false);
        ApplyTargetGridSizing(allyTargetGrid);
        ApplyTargetGridSizing(enemyTargetGrid);
        GameObject firstSpawned = null;
        foreach(MenuOption target in allyOptions)
        {
            GameObject entry = BuildEntry(target, allyTargetGrid, targetFontSize, (opt) => opt.onSelect());
            targetOptionMap[entry] = target;
            spawnedTargets.Add(entry);
            if(firstSpawned == null) firstSpawned = entry;
        }
        foreach(MenuOption target in enemyOptions)
        {
            GameObject entry = BuildEntry(target, enemyTargetGrid, targetFontSize, (opt) => opt.onSelect());
            targetOptionMap[entry] = target;
            spawnedTargets.Add(entry);
            if(firstSpawned == null) firstSpawned = entry;
        }
        UpdatePathText(BreadcrumbPrefix(), BreadcrumbSuffix());
        if(firstSpawned != null)
        {
        EventSystem.current?.SetSelectedGameObject(null);
        EventSystem.current?.SetSelectedGameObject(spawnedTargets[0]);
        MoveCursor(spawnedTargets[0]);
        }
    }
    void EndTargeting()
    {
        isTargeting = false;
        targetingBreadcrumbSuffix = "";
        ClearSpawnedTargets();
        if(leftPanelGroup != null){leftPanelGroup.alpha = 1f; leftPanelGroup.interactable = true;}
        if(targetGrid != null) targetGrid.gameObject.SetActive(false);
        if(detailText != null) detailText.gameObject.SetActive(true);
        UpdatePathText(BreadcrumbPrefix(), BreadcrumbSuffix());
        GameObject currentLeft = spawnedEntries.Find(entry => entry.GetComponent<Button>().interactable);
        if(currentLeft != null)
        {
            EventSystem.current?.SetSelectedGameObject(currentLeft);
            EntryHighlight(currentLeft);
        }
    }
    void FinalizeTurn()
    {
      actionSelected = true;
      screenHistory.Clear();
      ShowMainMenu();
      DisableMenu();  
    }
    void ClearSpawnedTargets() 
    {
        foreach(var entry in spawnedTargets) Destroy(entry); 
        spawnedTargets.Clear();
        targetOptionMap.Clear();
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

