using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "New Item", menuName = "Create New Item")]
public class Item : ScriptableObject
{
    public int id;
    public string itemName;
    public string description;
    public int value;
    public bool canBeUsed { get; set; }
    public List <Effect> effects = new List <Effect>();
    public int sellPrice = 0;
    public int buyPrice = 0;
}