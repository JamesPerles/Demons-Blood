using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public abstract class SubMenu : MenuBase
{
protected Stack<MenuScreen> screenHistory = new Stack<MenuScreen>();
protected virtual void Update()
    {
        if(menuDisplay != null && !menuDisplay.activeSelf) return;
        if(Input.GetKeyDown(KeyCode.Backspace))
        {
            if(screenHistory.Count > 1) PreviousScreen();
            else Close();
        }
    }
    public abstract void Close();
    protected void OpenScreen(List<MenuOption> options, string title = "", bool usePaging = true)
    {
        MenuScreen screen = new MenuScreen(options, fontSize, 1, cellSize, spacing, title);
        screen.usePaging = usePaging;
        screenHistory.Push(screen);
        FillMenu(screen);
    }
    protected void RefreshScreen(List<MenuOption> newOptions)
    {
        if(screenHistory.Count == 0) return;
        MenuScreen current = screenHistory.Peek();
        current.allOptions = newOptions;
        FillMenu(current);
    }
    protected void PopAndRefresh(List<MenuOption> newOptions, int levels = 1)
    {
        for(int i = 0; i < levels; i++)
        {
            if(screenHistory.Count == 0) {Close(); return;}
            screenHistory.Pop();
        }
        if(screenHistory.Count == 0) {Close(); return;}
        screenHistory.Peek().allOptions = newOptions;
        FillMenu(screenHistory.Peek());
    }
    protected void PreviousScreen()
    {
        screenHistory.Pop();
        if(screenHistory.Count == 0) {Close(); return;}
        FillMenu(screenHistory.Peek());
    }
    protected void NextPage()
    {
        if(screenHistory.Count == 0) return;
        MenuScreen screen = screenHistory.Peek();
        if(!screen.usePaging) return;
        int maxPage = Mathf.Max(0, (screen.allOptions.Count - 1) / entriesPerPage);
        screen.currentPage = screen.currentPage >= maxPage ? 0 : screen.currentPage + 1;
        FillMenu(screen);
    }
protected void FillMenu(MenuScreen screen)
{
    ClearEntries();
    ApplyGridSizing(screen.columns);
    UpdatePathText(BreadcrumbPrefix(), BreadcrumbSuffix());
    if(screen.allOptions.Count == 0)
    {
        EmptyMenu(screen.fontSize);
        return;
    }
    int start = screen.usePaging ? screen.currentPage * entriesPerPage : 0;
    int count = screen.usePaging ? Mathf.Min(entriesPerPage, screen.allOptions.Count - start) : screen.allOptions.Count;
    for (int i = start; i < start + count; i++)
    SpawnEntry(screen.allOptions[i], optionsGrid, screen.fontSize, SelectCommand);
    GameObject firstEnabled = spawnedEntries.Find(e => {var b = e.GetComponent<UnityEngine.UI.Button>(); 
    return b != null && b.interactable;});
    if(firstEnabled != null)
    {
        EventSystem.current?.SetSelectedGameObject(firstEnabled);
        EntryHighlight(firstEnabled);
    }
}
protected virtual void SelectCommand(MenuOption option)
    {
        if(option.getChildren != null) OpenScreen(option.getChildren(), option.label, option.childUsePaging);
        else option.onSelect();
    }
protected void UpdatePathText(string prefix = "", string suffix = "")
{
    if(pathText == null) return;
    List<string> historyParts = new List<string>();
    foreach (MenuScreen screen in screenHistory) historyParts.Add(screen.title);
    historyParts.Reverse();
    List<string> all = new List<string>();
    if(!string.IsNullOrEmpty(prefix)) all.Add(prefix);
    all.AddRange(historyParts);
    if(!string.IsNullOrEmpty(suffix)) all.Add(suffix);
    all.RemoveAll(string.IsNullOrEmpty);
    pathText.text = string.Join(" > ", all);
}
protected virtual string BreadcrumbPrefix() => "";
protected virtual string BreadcrumbSuffix() => "";
}