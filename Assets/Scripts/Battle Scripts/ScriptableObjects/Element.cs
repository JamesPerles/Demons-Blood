using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public enum Element
{
None, Light, Dark, Fire, Ice, Lightning, 
Wind, Water, Earth, Nature, Poison, Sand
}
[System.Serializable]
public class ElementMatchup
{
    public Element attacker;
    public List<Element> defenders = new List<Element>();
    public float multiplier = 1f;
}
[CreateAssetMenu(fileName = "Element Chart", menuName = "Element Chart")]
public class ElementChart : ScriptableObject
{
    public List<ElementMatchup> matchups = new List<ElementMatchup>();
    public float GetMultiplier(Element attacker, Element defender)
    {
        bool attackerIsLightOrDark = attacker == Element.Light || attacker == Element.Dark;
        bool defenderIsLightOrDark = defender == Element.Light || defender == Element.Dark;
            if(attackerIsLightOrDark)
            {
                if(attacker == defender) return 0.5f;
                if(defenderIsLightOrDark) return 2f;
                return 1.5f;
            }
            ElementMatchup match = matchups.Find(entry => entry.attacker == attacker && entry.defenders.Contains(defender));
            return match != null ? match.multiplier : 1f;
        }
    }
