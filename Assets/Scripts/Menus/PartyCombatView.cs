using UnityEngine;
using TMPro;
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
public GameObject divider;
}
public class PartyCombatView : MonoBehaviour
    {
        public PartyMemberSlot slot;
    }

