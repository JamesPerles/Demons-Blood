using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class MenuOption
{
    public string label;
    public string description = "";
    public System.Action onSelect;
    public System.Func<List<MenuOption>> getChildren;
    public bool enabled = true;
    public Color? textColor;
    public enum TargetScope {None, Enemies, Allies, Any, Self}
    public TargetScope targetScope = TargetScope.None;
    public System.Action<ICombatant> onTargetSelect;
    public bool childUsePaging = true;
    public int childColumns = 1;
    public MenuOption(string label, System.Action onSelect)
    {this.label = label; this.onSelect = onSelect;}
    public MenuOption(string label, System.Func<List<MenuOption>> getChildren)
    {this.label = label; this.getChildren = getChildren;}
    }
public class MenuScreen
{
    public List<MenuOption> allOptions;
    public float fontSize;
    public int columns;
    public Vector2 cellSize;
    public Vector2 spacing;
    public int currentPage;
    public string title;
    public bool usePaging = true;
    public MenuScreen(List<MenuOption> allOptions, float fontSize, int columns, Vector2 cellSize, Vector2 spacing, string title = "")
    {
        this.allOptions = allOptions;
        this.fontSize = fontSize;
        this.columns = columns;
        this.cellSize = cellSize;
        this.spacing = spacing;
        this.currentPage = 0;
        this.title = title;
    }
}
public abstract class MenuBase : MonoBehaviour
{
public GameObject menuDisplay;
public Transform optionsGrid;
public GameObject entryPrefab;
public GridLayoutGroup grid;
public float fontSize = 32f;
public Vector2 cellSize = new Vector2(280f, 60f);
public Vector2 spacing = new Vector2(0f, 8f);
public float minAutoFontSize = 14f;
public int entriesPerPage = 20;
public RectTransform cursor;
public float cursorOffset = 16f;
public TextMeshProUGUI pathText;
public TextMeshProUGUI detailText;
public TextMeshProUGUI pageText;
protected List<GameObject> spawnedEntries = new List<GameObject>();
protected Dictionary<GameObject, MenuOption> entryOptionMap = new Dictionary<GameObject, MenuOption>();
protected void SetDisplayActive(bool active)
    {
        if(menuDisplay != null) menuDisplay.SetActive(active);
        else gameObject.SetActive(active);
    }
    protected void ClearEntries()
    {
        foreach (var entry in spawnedEntries) Destroy(entry);
        spawnedEntries.Clear();
        entryOptionMap.Clear();
    }
    protected void ApplyGridSizing(int columns)
    {
        if(grid == null) return;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.cellSize = cellSize;
        grid.spacing = spacing;
    }
    protected int GetPerPage(MenuScreen screen)
    {
        return screen.columns > 1 ? screen.columns * 3 : entriesPerPage;
    }
    protected void UpdatePageText(MenuScreen screen)
    {
        if(pageText == null) return;
        if(!screen.usePaging) {pageText.text = ""; return;}
        int perPage = GetPerPage(screen);
        int maxPage = Mathf.Max(1, Mathf.CeilToInt((float)screen.allOptions.Count / perPage));
        pageText.text = maxPage > 1 ? $"{screen.currentPage + 1}/{maxPage}" : "";
    }
    protected GameObject BuildEntry(MenuOption option, Transform parent, float entryFontSize, System.Action<MenuOption> onClick)
    {
        GameObject entry = Instantiate(entryPrefab, parent);
        var label = entry.GetComponentInChildren<TextMeshProUGUI>();
        if(label != null)
        {
            label.text = option.label;
            label.enableAutoSizing = true;
            label.fontSizeMax = entryFontSize;
            label.fontSizeMin = Mathf.Min(minAutoFontSize, entryFontSize);
            if(option.textColor.HasValue) label.color = option.textColor.Value;
        }
        Button button = entry.GetComponent<Button>();
        if(button != null)
        {
            button.interactable = option.enabled;
            button.onClick.AddListener(() => onClick(option));
            Image targetGraphic = button.targetGraphic as Image;
            if(targetGraphic != null)
            targetGraphic.color = option.enabled ? button.colors.normalColor : button.colors.disabledColor;
        }
        var relay = entry.GetComponent<MenuEntrySelect>();
        if(relay == null) relay = entry.AddComponent<MenuEntrySelect>();
        relay.owner = this;
        return entry;
    }
    protected GameObject SpawnEntry(MenuOption option, Transform parent, float entryFontSize, System.Action<MenuOption> onClick)
    {
        GameObject entry = BuildEntry(option, parent, entryFontSize, onClick);
        entryOptionMap[entry] = option;
        spawnedEntries.Add(entry);
        return entry;
    }
    public void RegisterEntry(GameObject entry, MenuOption option)
    {
        entryOptionMap[entry] = option;
        if(!spawnedEntries.Contains(entry)) spawnedEntries.Add(entry);
        MenuEntrySelect relay = entry.GetComponent<MenuEntrySelect>();
        if(relay == null) relay = entry.AddComponent<MenuEntrySelect>();
        relay.owner = this;
    }
    protected void EmptyMenu(float fontSize)
    {
        GameObject emptyEntry = Instantiate(entryPrefab, optionsGrid);
        var label = emptyEntry.GetComponentInChildren<TextMeshProUGUI>();
        if(label != null)
        {
            label.text = "Empty"; 
            label.enableAutoSizing = true;
            label.fontSizeMax = fontSize;
            label.fontSizeMin = Mathf.Min(minAutoFontSize, fontSize);
        }
        emptyEntry.GetComponent<Button>().interactable = false;
        spawnedEntries.Add(emptyEntry);
        if(cursor != null) cursor.gameObject.SetActive(false);
        if(detailText != null) detailText.text = ""; 
    }
    public virtual void EntryHighlight(GameObject entry)
    {
        if(!entryOptionMap.TryGetValue(entry, out MenuOption option)) return;
        if(detailText != null) detailText.text = option.description;
        MoveCursor(entry);
    }
     protected void MoveCursor(GameObject entry)
    {
        if(cursor == null || entry == null) return;
        RectTransform entryRect = entry.GetComponent<RectTransform>();
        if(entryRect == null) return;
        cursor.gameObject.SetActive(true);
        cursor.position = new Vector3 (
        entryRect.position.x - (entryRect.rect.width * entryRect.lossyScale.x / 2f) 
        - cursorOffset, entryRect.position.y, cursor.position.z);
    }
}
