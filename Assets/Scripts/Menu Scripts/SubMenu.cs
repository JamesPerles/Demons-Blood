using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
    protected void OpenScreen(List<MenuOption> options)
    {
        MenuScreen screen = new MenuScreen(options, fontSize, 1, cellSize, spacing);
        screenHistory.Push(screen);
        FillMenu(screen);
    }
    protected void PreviousScreen()
    {
        screenHistory.Pop();
        if(screenHistory.Count == 0) {Close(); return;}
        FillMenu(screenHistory.Peek());
    }
protected void FillMenu(MenuScreen screen)
{
    ClearEntries();
    ApplyGridSizing(screen.columns);
    if(screen.allOptions.Count == 0)
    {
        EmptyMenu(screen.fontSize);
        return;
    }
    foreach (MenuOption option in screen.allOptions)
        {
            GameObject entry = Instantiate(entryPrefab, optionsGrid);
            var label = entry.GetComponentInChildren<TextMeshProUGUI>();
            if(label != null) {label.text = option.label; label.fontSize = screen.fontSize;}
            MenuOption captured = option;
            Button button = entry.GetComponent<Button>();
            button.interactable = option.enabled;
            button.onClick.AddListener(() => SelectCommand(captured));
            spawnedEntries.Add(entry);
        }
}
void SelectCommand(MenuOption option)
    {
        if(option.getChildren != null) OpenScreen(option.getChildren());
        else option.onSelect();
    }
protected void EmptyMenu(float fontSize)
    {
        GameObject emptyEntry = Instantiate(entryPrefab, optionsGrid);
        var label = emptyEntry.GetComponentInChildren<TextMeshProUGUI>();
        if(label != null) {label.text = "Empty"; label.fontSize = fontSize;} 
        emptyEntry.GetComponent<Button>().interactable = false;
        spawnedEntries.Add(emptyEntry);
    }
}