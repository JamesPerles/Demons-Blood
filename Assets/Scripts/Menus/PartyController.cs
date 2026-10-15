using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class PartyController : MonoBehaviour, ICardHighlightHandler, IPageableTab
{
    public GameObject slotPrefab;
    public Transform slotParent;
    public int columns = 2;
    public int rows = 3;
    public float rowSpacing = 50f;
    public Color cardBorderDefault = new Color32(0x3A, 0x16, 0x16, 0xFF);
    public Color cardBorderSelected = new Color32(0xD8, 0x5A, 0x30, 0xFF);
    public Color cardBackgroundSelected = new Color32(0x24, 0x10, 0x10, 0xFF);
    public Color aliveNameColor = new Color32(0xC9, 0xC2, 0xC2, 0xFF);
    public Color deadNameColor = new Color32(0xE2, 0x4B, 0x4A, 0xFF);
    public Color pickedUpNameColor = new Color32(0xF2, 0xF2, 0xF2, 0xFF);
    public Color hpColor = new Color32(0x63, 0x99, 0x22, 0xFF);
    public Color mpColor = new Color32(0x37, 0x8A, 0xDD, 0xFF);
    List<GameObject> spawnedSlots = new List<GameObject>();
    List<GameObject> orderedCharacters = new List<GameObject>();
    List<GameObject> allCharacters = new List<GameObject>();
    List<Transform> spawnedRows = new List<Transform>();
    Transform currentRow;
    int cardsInCurrentRow;
    int currentPage;
    PauseMenu owner;
    System.Action <GameObject> onConfirmCharacter;
    GameObject pickedUpCharacter;
    public bool HasPickedUp => pickedUpCharacter != null;
    int PerPage => Mathf.Max(1, columns) * Mathf.Max(1, rows);
    public int MaxPage => allCharacters.Count == 0 ? 0 : (allCharacters.Count - 1) / PerPage;
    public void Init(PauseMenu owner)
    {
        this.owner = owner;
    }
    public void Refresh(System.Action<GameObject> onConfirm)
    {
        onConfirmCharacter = onConfirm;
        if(PlayerParty.instance == null || slotPrefab == null || slotParent == null) return;
        allCharacters = new List<GameObject>(PlayerParty.instance.playableCharacters); 
        currentPage = Mathf.Clamp(currentPage, 0, MaxPage);
        RebuildPage();
    }
    void RebuildPage()
    {
        ClearSlots();
        int start = currentPage * PerPage;
        int count = Mathf.Min(PerPage, allCharacters.Count - start);
        for(int i = start; i < start + count; i++) SpawnSlot(allCharacters[i]);
        if(owner != null && owner.pageText != null)
        owner.pageText.text = MaxPage > 0 ? $"page {currentPage + 1} / {MaxPage + 1}" : "";
        if(spawnedSlots.Count > 0 && owner != null) owner.FocusEntry(spawnedSlots[0]);
    }
    public void NextPage()
    {
        currentPage = currentPage >= MaxPage ? 0 : currentPage + 1;
        RebuildPage();
    }
    public void PreviousPage()
    {
        currentPage = currentPage <= 0 ? MaxPage : currentPage - 1;
        RebuildPage();
    }
    public void ClearSlots()
    {
        foreach(GameObject slot in spawnedSlots)
        {
            if(slot == null) continue;
            owner?.RemoveEntry(slot);
            Destroy(slot);
        }
        spawnedSlots.Clear();
        orderedCharacters.Clear();
        foreach(Transform row in spawnedRows) if(row != null) Destroy(row.gameObject);
        spawnedRows.Clear();
        currentRow = null;
        cardsInCurrentRow = 0;
    }
    Transform GetRow()
    {
        if(currentRow == null || cardsInCurrentRow >= Mathf.Max(1, columns))
        {
            GameObject rowObj = new GameObject("Row", typeof(RectTransform));
            rowObj.transform.SetParent(slotParent, false);
            RectTransform rowRect = rowObj.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(0f, 1f);
            rowRect.pivot = new Vector2(0f, 1f);
            HorizontalLayoutGroup rowLayout = rowObj.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = rowSpacing;
            rowLayout.childAlignment = TextAnchor.UpperLeft;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childScaleWidth = true;
            rowLayout.childScaleHeight = true;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            currentRow = rowObj.transform;
            spawnedRows.Add(currentRow);
            cardsInCurrentRow = 0;
        }
        cardsInCurrentRow++;
        return currentRow;
    }
            GameObject SpawnSlot(GameObject characterObject)
            {
            ActiveStats stats = characterObject.GetComponent<ActiveStats>();
            if(stats == null || slotPrefab == null || slotParent == null) return null;
            Transform row = GetRow();
            GameObject spawned = Instantiate(slotPrefab, row);
            spawnedSlots.Add(spawned);
            orderedCharacters.Add(characterObject);
            PartyMenuView view = spawned.GetComponent<PartyMenuView>();
            if(view == null) return spawned;
            bool isDead = stats.currentHP <= 0;
            bool isPickedUp = pickedUpCharacter ==characterObject;
            if(view.nameText != null)
            {
                view.nameText.text = stats.currentName;
               view.nameText.color = isPickedUp ? pickedUpNameColor : (isDead ? deadNameColor : aliveNameColor);
            }
            if(view.levelText != null) view.levelText.text = $"LV {stats.currentLevel}";
            SetBar(view.hpFill, view.hpText, "HP", stats.currentHP, stats.finalHP, hpColor);
            SetBar(view.mpFill, view.mpText, "MP", stats.currentMP, stats.finalMP, mpColor);
            GameObject capturedCharacter = characterObject;
            GameObject capturedSlot = spawned;
            MenuOption option = new MenuOption(stats.currentName, () => { });
            if(owner != null) owner.RegisterEntry(spawned, option);
            if(view.button != null)
            {
                view.button.onClick.RemoveAllListeners();
                view.button.onClick.AddListener(() =>
                {
                    if(owner != null) owner.FocusEntry(capturedSlot);
                     HandleConfirm(capturedCharacter);
            });
        } 
        SetCardVisual(view, isPickedUp);
        UpdateRowSize(row);
        return spawned;
    }
    void UpdateRowSize(Transform row)
    {
        RectTransform rowRect = row as RectTransform;
        if(rowRect == null) return;
        float totalWidth = 0f;
        float maxHeight = 0f;
        int childCount = row.childCount;
        for(int i = 0; i < childCount; i++)
        {
            RectTransform childRect = row.GetChild(i) as RectTransform;
            if(childRect == null) continue;
            totalWidth += childRect.rect.width * childRect.localScale.x;
            maxHeight = Mathf.Max(maxHeight, childRect.rect.height * childRect.localScale.y);
        }
        if(childCount > 1) totalWidth += rowSpacing * (childCount - 1);
        rowRect.sizeDelta = new Vector2(totalWidth, maxHeight);
    }
    public void SelectedCharacter(GameObject character)
    {
        if(character == null) return;
        ClearSlots();
        SpawnSlot(character);
        if(spawnedSlots.Count == 0) return;
        PartyMenuView view = spawnedSlots[0].GetComponent<PartyMenuView>();
        if(view == null) return;
        SetCardVisual(view, true);
        if(view.button != null) view.button.interactable = false;
    }
    void HandleConfirm(GameObject character)
    {
        if(pickedUpCharacter != null)
        {
            if(pickedUpCharacter != character) PlayerParty.instance.Swap(pickedUpCharacter, character);
            pickedUpCharacter = null;
            Refresh(onConfirmCharacter);
            return;
        }
        onConfirmCharacter?.Invoke(character);
    }
    public void ToggleSwapOnFocused(GameObject focusedEntry)
    {
        int index = spawnedSlots.IndexOf(focusedEntry);
        if(index < 0 || index >= orderedCharacters.Count) return;
        GameObject character = orderedCharacters[index];
        pickedUpCharacter = pickedUpCharacter == character ? null : character;
        Refresh(onConfirmCharacter);
    }
    public void CancelSwap()
    {
        pickedUpCharacter = null;
        Refresh(onConfirmCharacter);
    }
     public void OnCardHighlighted(GameObject entry)
    {
        for(int i = 0; i < spawnedSlots.Count; i++)
        {
            if(spawnedSlots[i] == null) continue;
            PartyMenuView view = spawnedSlots[i].GetComponent<PartyMenuView>();
            if(view == null) continue;
            bool isPickedUp = i < orderedCharacters.Count && orderedCharacters [i] == pickedUpCharacter;
            SetCardVisual(view, spawnedSlots[i] == entry || isPickedUp);
    }
    }
    void SetCardVisual(PartyMenuView view, bool selected)
    {
        if(view.borderImage != null) view.borderImage.color = selected ? cardBorderSelected : cardBorderDefault;
        if(view.backgroundImage != null)
        {
            Color bg = cardBackgroundSelected;
            bg.a = selected ? 1f : 0f;
            view.backgroundImage.color = bg;
        }
    }
    void SetBar(Image fill, TextMeshProUGUI label, string prefix, int current, int max, Color color)
    {
        if(fill != null) 
        {
            fill.fillAmount = max > 0 ? Mathf.Clamp01((float) current / max) : 0f;
            fill.color = color;
        }
        if(label != null) label.text = $"{prefix}: {current}/{max}";
    }
}
