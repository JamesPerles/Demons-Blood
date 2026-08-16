using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "New Shop", menuName = "Shop/Shop Stats")]
public class ShopStats : ScriptableObject
{
public string shopName;
public List<Item> itemsForSale = new List<Item>();
public List<Equipment> equipmentForSale = new List<Equipment>();
}
