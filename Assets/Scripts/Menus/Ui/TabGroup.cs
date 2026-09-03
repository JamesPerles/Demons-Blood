using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
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
        tabs[index].onSelected?.Invoke();
    }
    void RefreshVisuals()
    {
        for(int i = 0; i < spawnedButtons.Count; i++)
        {
            TabButtonView view = spawnedButtons[i].GetComponent<TabButtonView>();
            TextMeshProUGUI label = view != null ? view.label : spawnedButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            bool isActive = i == currentIndex;
            if(label != null) label.color = isActive ? activeColor : inactiveColor;
            if(view != null && view.underline != null) view.underline.SetActive(isActive);
        }
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
