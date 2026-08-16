using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "New Fusion", menuName = "Create New Fusion")]
public class Fusion : Learnable
{
public string fusionName;
    public Element element;
    public int Damage = 0;
    public int HPCost = 0;
    public int MPCost = 0;
    public List<Effect> effects = new List<Effect>();
    public bool isAOE = false;
}
