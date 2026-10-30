using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class TabDefinition
{
    public string label;
    public Action onSelected;
    public TabDefinition(string label, Action onSelected)
{
    this.label = label;
    this.onSelected = onSelected;
}
}
public class TabGroup : MonoBehaviour
{
    public GameObject tabButtonPrefab;
    public Transform tabButtonParent;
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(0.6f, 0.55f, 0.55f);
    public Color focusedColor = new Color32(0xD8, 0x5A, 0x30, 0xFF);
    List<TabDefinition> tabs = new List<TabDefinition>();
    List<GameObject> spawnedButtons = new List <GameObject>();
    public int currentIndex {get; private set;} = -1;
    public int tabCount => tabs.Count;
    public void SetTabs(List<TabDefinition> newTabs, int startIndex = 0)
    {
        ClearButtons();
        tabs = newTabs ?? new List<TabDefinition>();
        currentIndex = -1;
        for(int i = 0; i < tabs.Count; i++)
        {
            int captured = i;
            GameObject button  = Instantiate(tabButtonPrefab, tabButtonParent);
            TabButtonView view = button.GetComponent<TabButtonView>();
            TextMeshProUGUI label = view != null ? view.label : button.GetComponentInChildren<TextMeshProUGUI>();
            if(label != null) label.text = tabs[i].label;
            if(view != null && view.underline != null) view.underline.SetActive(false);
            Button btn = button.GetComponent<Button>();
            if(btn != null) 
            {
                btn.navigation = new Navigation { mode = Navigation.Mode.Automatic };
                btn.transition = Selectable.Transition.None;
            }
            button.AddComponent<TabHoverRelay>();
            if(btn != null) btn.onClick.AddListener(() => SelectTab(captured));
            spawnedButtons.Add(button);
        }
        if(tabs.Count > 0) SelectTab(Mathf.Clamp(startIndex, 0, tabs.Count - 1), forceInvoke: true);
    }
    void ClearButtons()
    {
        foreach(GameObject button in spawnedButtons) Destroy(button);
        spawnedButtons.Clear();
    }
    public void SelectTab(int index, bool forceInvoke = false)
    {
        if(index < 0 || index >= tabs.Count) return;
        if(index == currentIndex && !forceInvoke) return;
        currentIndex = index;
        RefreshVisuals();
        EventSystem.current?.SetSelectedGameObject(null);
        tabs[index].onSelected?.Invoke();
        FocusButton(index);
    }
    void FocusButton(int index)
    {
        if(EventSystem.current == null || index < 0 || index >= spawnedButtons.Count) return;
        GameObject button = spawnedButtons[index];
        if(button != null && button.activeInHierarchy) EventSystem.current.SetSelectedGameObject(button);
    }
    void RefreshVisuals()
    {
        for(int i = 0; i < spawnedButtons.Count; i++)
        {
            TabButtonView view = spawnedButtons[i].GetComponent<TabButtonView>();
            TextMeshProUGUI label = view != null ? view.label : spawnedButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            bool isActive = i == currentIndex;
            bool isFocused = EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null
            && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(spawnedButtons[i].transform);
            if(label != null) label.color = isFocused ? focusedColor : (isActive ? (SettingsManager.instance != null ? SettingsManager.instance.TextSelectedColor : activeColor) : (SettingsManager.instance != null ? SettingsManager.instance.TextMutedColor : inactiveColor));
            if(view != null && view.underline != null) view.underline.SetActive(isActive);
        }
    }
    void Start()
    {
        if (SettingsManager.instance != null) SettingsManager.instance.OnTextColorChanged += OnThemeChanged;
    }
    void OnDestroy()
    {
        if (SettingsManager.instance != null) SettingsManager.instance.OnTextColorChanged -= OnThemeChanged;
    }
    void OnThemeChanged(Color color) { RefreshVisuals(); }
    GameObject lastSeenSelection;
    void Update()
    {
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if(selected == lastSeenSelection) return;
        lastSeenSelection = selected;
        RefreshVisuals();
    }
    public GameObject CurrentButton => currentIndex >= 0 && currentIndex < spawnedButtons.Count ? spawnedButtons[currentIndex] : null;
    public bool OwnsButton(GameObject candidate)
    {
        if(candidate == null) return false;
        foreach(GameObject button in spawnedButtons)
        if(button != null && candidate.transform.IsChildOf(button.transform)) return true;
        return false;
    }
    public GameObject GetNeighborButton(GameObject current, int direction)
    {
        if(current == null) return null;
        for(int i = 0; i < spawnedButtons.Count; i++)
        {
            GameObject button = spawnedButtons[i];
            if(button == null || !current.transform.IsChildOf(button.transform)) continue;
            int target = i + direction;
            if(target < 0 || target >= spawnedButtons.Count) return null;
            GameObject neighbor = spawnedButtons[target];
            return neighbor != null && neighbor.activeInHierarchy ? neighbor : null;
        }
        return null;
    }
    public void NextTab()
    {
        if(tabs.Count == 0) return;
        SelectTab((currentIndex + 1) % tabs.Count);
    }
    public void PreviousTab()
    {
        if(tabs.Count == 0) return;
        SelectTab((currentIndex - 1 + tabs.Count) % tabs.Count);
    }
    public void ReloadCurrentTab()
    {
        if(currentIndex < 0 || currentIndex >= tabs.Count) return;
        tabs[currentIndex].onSelected?.Invoke();
    }
    public void Hide() => gameObject.SetActive(false);
    public void Show() => gameObject.SetActive(true);
}
public class TabHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    bool pointerInside;
    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        TryHoverFocus();
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
    }
    void OnDisable() 
    {
        pointerInside = false;
    }
    void Update()
    {
        bool moved = MenuSelect.MouseMoved();
        if(pointerInside && moved) TryHoverFocus();
    }
    void TryHoverFocus()
    {
        if(!MenuSelect.MouseMoved()) return;
        EventSystem events = EventSystem.current;
        if(events != null && events.currentSelectedGameObject != gameObject) events.SetSelectedGameObject(gameObject);
    }
}
