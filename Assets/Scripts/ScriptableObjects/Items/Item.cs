using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "New Item", menuName = "Create New Item")]
public class Item : Baggable
{
    public string itemName;
    public string description;
    public enum ItemType {Consumable, KeyItem}
    public ItemType itemType = ItemType.Consumable;
    public List <Effect> effects = new List <Effect>();
    public int sellPrice = 0;
    public int buyPrice = 0;
    public override string DisplayName => itemName;
}