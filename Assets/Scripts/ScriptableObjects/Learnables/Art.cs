using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "New Art", menuName = "Create New Art")]
public class Art : Learnable
{
    public string artName;
    public int Damage = 0; 
    public int Cost = 0;
    public List<Effect> effects = new List<Effect>();
    public bool isAOE = false;
}
