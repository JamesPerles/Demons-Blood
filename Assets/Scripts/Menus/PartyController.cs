using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class PartyController : MonoBehaviour, ICardHighlightHandler
{
    public GameObject slotPrefab;
    public Transform slotParent;
    public ScrollRect rosterScrollRect;
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
    PauseMenu owner;
    System.Action <GameObject> onConfirmCharacter;
    GameObject pickedUpCharacter;
    public bool HasPickedUp => pickedUpCharacter != null;
    public void Init(PauseMenu owner)
    {
        this.owner = owner;
    }
    public void Refresh(System.Action<GameObject> onConfirm)
    {
        onConfirmCharacter = onConfirm;
        if(PlayerParty.instance == null || slotPrefab == null || slotParent == null) return;
        foreach(GameObject slot in spawnedSlots) Destroy(slot);
        spawnedSlots.Clear();
        orderedCharacters.Clear();
        GameObject[] roster = PlayerParty.instance.playableCharacters;
        foreach(GameObject characterObject in roster) SpawnSlot(characterObject);
        if(spawnedSlots.Count > 0 && owner != null) owner.EntryHighlight(spawnedSlots[0]);
    }
            GameObject SpawnSlot(GameObject characterObject)
            {
            ActiveStats stats = characterObject.GetComponent<ActiveStats>();
            if(stats == null || slotPrefab == null || slotParent == null) return null;
            GameObject spawned = Instantiate(slotPrefab, slotParent);
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
                    if(owner != null) owner.EntryHighlight(capturedSlot);
                     HandleConfirm(capturedCharacter);
            });
        } 
        SetCardVisual(view, isPickedUp);
        return spawned;
    }
    public void SelectedCharacter(GameObject character)
    {
        if(character == null) return;
        int existingIndex = orderedCharacters.IndexOf(character);
        bool cardIsAlive = existingIndex >= 0 && spawnedSlots[existingIndex] != null;
        if(!cardIsAlive)
        {
            for(int i = spawnedSlots.Count - 1; i >= 0; i--)
            {
                if(spawnedSlots[i] != null) Destroy(spawnedSlots[i]);
            }
            spawnedSlots.Clear();
            orderedCharacters.Clear();
            SpawnSlot(character);
        }
        else
        {
        for(int i = spawnedSlots.Count - 1; i >= 0; i--)
        {
            if(i >= orderedCharacters.Count || orderedCharacters[i] == character) continue;
            if(spawnedSlots[i] != null) Destroy(spawnedSlots[i]);
            spawnedSlots.RemoveAt(i);
            orderedCharacters.RemoveAt(i);
        }
        }
        if(spawnedSlots.Count == 0) return;
        spawnedSlots[0].transform.SetSiblingIndex(0);
        PartyMenuView view = spawnedSlots[0].GetComponent<PartyMenuView>();
        if(view == null) return;
        SetCardVisual(view, true);
        if(view.button != null) view.button.interactable = false;
    }
    void ResetScrollToTop()
    {
        if(rosterScrollRect == null && slotParent != null) rosterScrollRect = slotParent.GetComponentInParent<ScrollRect>();
        if(rosterScrollRect != null) rosterScrollRect.verticalNormalizedPosition = 1f;
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
