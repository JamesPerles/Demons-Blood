using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
[System.Serializable]
public class PartyMemberSlot
{
public GameObject root;
public Image icon;
public TextMeshProUGUI nameText;
public TextMeshProUGUI levelText;
public Image hpFill;
public TextMeshProUGUI hpText;
public Image mpFill;
public TextMeshProUGUI mpText;
public Image transformFill;
public TextMeshProUGUI transformText;
public GameObject activeHighlight;
public GameObject divider;
}
public class PartyMemberSlotView : MonoBehaviour
    {
        public PartyMemberSlot slot;
    }
public class PartyStatusUI : MonoBehaviour
    {
        public GameObject slotPrefab;
        public Transform slotParent;
        public Color aliveNameColor = Color.white;
        public Color deadNameColor = new Color(0.6f, 0.15f, 0.15f);
        public RectTransform borderRect;
        List<GameObject> spawnedSlots = new List<GameObject>();
        public void Refresh(List<ActiveStats> party, ActiveStats activeMember)
        {
            foreach(GameObject spawned in spawnedSlots) Destroy(spawned);
            spawnedSlots.Clear();
            if(party == null || slotPrefab == null || slotParent == null) return;
            List<ActiveStats> livingRoster = new List<ActiveStats>();
            foreach(var member in party) if(member != null) livingRoster.Add(member);
            for(int i = 0; i < livingRoster.Count; i++)
            {
                ActiveStats member = livingRoster[i];
                GameObject spawned = Instantiate(slotPrefab, slotParent);
                spawnedSlots.Add(spawned);
                PartyMemberSlotView view = spawned.GetComponent<PartyMemberSlotView>();
                if(view == null || view.slot == null) continue;
                PartyMemberSlot slot = view.slot;
                bool isDead = member.currentHP <= 0;
                if(slot.root != null) slot.root.SetActive(true);
                if(slot.nameText != null)
                {
                    slot.nameText.text = member.currentName;
                    slot.nameText.color = isDead ? deadNameColor : aliveNameColor;
                }
                if(slot.levelText != null) slot.levelText.text = $"LV {member.currentLevel}";
                SetBar(slot.hpFill, slot.hpText, member.currentHP, member.finalHP);
                SetBar(slot.mpFill, slot.mpText, member.currentMP, member.finalMP);
                SetTransformBar(slot.transformFill, slot.transformText, member.transformGauge, member.transformGaugeMax);
                if(slot.activeHighlight != null) slot.activeHighlight.SetActive(!isDead && member == activeMember);
                bool isLast = i == livingRoster.Count - 1;
                if(slot.divider != null) slot.divider.SetActive(!isLast);
            }
        if(borderRect != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(borderRect);
        }
        }
void SetBar(Image fill, TextMeshProUGUI label, int current, int max)
    {
        if(fill != null) fill.fillAmount = max > 0 ? Mathf.Clamp01((float) current / max) : 0f;
        if(label != null) label.text = $"{current}/{max}";
    }
    void SetTransformBar(Image fill, TextMeshProUGUI label, float current, float max)
    {
        float percent = max > 0 ? Mathf.Clamp01(current / max) : 0f;
        if(fill != null) fill.fillAmount = percent;
        if(label != null) label.text = $"{Mathf.RoundToInt(percent * 100f)}%";
    }
}