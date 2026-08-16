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
}
public class PartyStatusUI : MonoBehaviour
    {
        public PartyMemberSlot[] slots = new PartyMemberSlot[4];
        public Color aliveNameColor = Color.white;
        public Color deadNameColor = new Color(0.6f, 0.15f, 0.15f);
        public void Refresh(List<ActiveStats> party, ActiveStats activeMember)
        {
            for(int i = 0; i < slots.Length; i++)
            {
                PartyMemberSlot slot = slots[i];
                if(slot == null) continue;
                bool hasMember = party != null && i < party.Count && party[i] != null;
                if(slot.root != null) slot.root.SetActive(hasMember);
                if(!hasMember) continue;
                ActiveStats member = party[i];
                bool isDead = member.currentHP <= 0;
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