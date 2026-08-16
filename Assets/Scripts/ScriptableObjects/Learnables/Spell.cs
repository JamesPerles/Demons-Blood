using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "New Spell", menuName = "Create New Spell")]
public class Spell : Learnable
{
    public string spellName;
    public Element element;
    public int Damage = 0;
    public int Cost = 0;
    public List<Effect> effects = new List<Effect>();
    public bool isAOE = false;
}
