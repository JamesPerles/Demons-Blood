using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class RosterController : MonoBehaviour
{
    public GameObject slotPrefab;
    public Transform slotParent;
    public Color aliveNameColor = Color.white;
    public Color deadNameColor = new Color(0.6f, 0.15f, 0.15f);
    public Color pickedUpColor = new Color(1f, 0.85f, 0.3f);
    List<GameObject> spawnedSlots = new List<GameObject>();
    List<GameObject> orderedCharacters = new List<GameObject>();
    MenuBase owner;
    System.Action <GameObject> onConfirmCharacter;
    GameObject pickedUpCharacter;
    public bool HasPickedUp => pickedUpCharacter != null;
    public void Init(MenuBase owner)
    {
        this.owner = owner;
    }
    public void Refresh(System.Action<GameObject> onConfirm)
    {
        onConfirmCharacter = onConfirm;
        if(PlayerParty.instance == null || slotPrefab == null || slotParent == null) return;
        spawnedSlots.RemoveAll(slot => slot == null);
        GameObject[] roster = PlayerParty.instance.playableCharacters;
        while(spawnedSlots.Count < roster.Length)
        spawnedSlots.Add(Instantiate(slotPrefab, slotParent));
        while(spawnedSlots.Count > roster.Length)
        {
            int lastIndex = spawnedSlots.Count - 1;
            Destroy(spawnedSlots[lastIndex]);
            spawnedSlots.RemoveAt(lastIndex);
        }
        orderedCharacters.Clear();
        for (int i = 0; i < roster.Length; i++)
        {
            GameObject characterObject = roster[i];
            orderedCharacters.Add(characterObject);
            GameObject spawned = spawnedSlots[i];
            spawned.transform.SetSiblingIndex(i);
            ActiveStats stats = characterObject.GetComponent<ActiveStats>();
            PartyMemberSlotView view = spawned.GetComponent<PartyMemberSlotView>();
            if(stats == null || view == null || view.slot == null) continue;
            PartyMemberSlot slot = view.slot;
            bool isDead = stats.currentHP <= 0;
            bool isPickedUp = pickedUpCharacter ==characterObject;
            if(slot.root != null) slot.root.SetActive(true);
            if(slot.nameText != null)
            {
                slot.nameText.text = stats.currentName;
                slot.nameText.color = isPickedUp ? pickedUpColor : (isDead ? deadNameColor : aliveNameColor);
            }
            if(slot.levelText != null) slot.levelText.text = $"LV {stats.currentLevel}";
            SetBar(slot.hpFill, slot.hpText, "HP", stats.currentHP, stats.finalHP);
            SetBar(slot.mpFill, slot.mpText, "MP", stats.currentMP, stats.finalMP);
            SetTransformBar(slot.transformFill, slot.transformText, stats.transformGauge, stats.transformGaugeMax);
            bool isLast = i == roster.Length - 1;
            if(slot.divider != null) slot.divider.SetActive(!isLast);
            GameObject capturedCharacter = characterObject;
            Button button = spawned.GetComponent<Button>();
            if(button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => HandleConfirm(capturedCharacter));
            }
            if(owner != null) owner.RegisterEntry(spawned, new MenuOption(stats.currentName, () => { }));
        }
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
    void SetBar(Image fill, TextMeshProUGUI label, string prefix, int current, int max)
    {
        if(fill != null) fill.fillAmount = max > 0 ? Mathf.Clamp01((float) current / max) : 0f;
        if(label != null) label.text = $"{prefix}: {current}/{max}";
    }
     void SetTransformBar(Image fill, TextMeshProUGUI label, float current, float max)
    {
        float percent = max > 0 ? Mathf.Clamp01(current / max) : 0f;
        if(fill != null) fill.fillAmount = percent; 
        if(label != null) label.text = $"Trans {Mathf.RoundToInt(percent * 100f)}%";
    }
}
