using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "New Equipment", menuName = "Create New Equipment")]
public class Equipment : ScriptableObject
{
    public string equipmentName;
    public int hp = 0;
    public int mp = 0;
    public int strength = 0;
    public int magic = 0;
    public int defense = 0;
    public int wisdom = 0;
    public int tech = 0;
    public int affinity = 0;
    public int speed = 0;
    public int luck = 0;
    public int Accuracy = 0;
    public int Evasion = 0;
    public int Precision = 0;
    public int Foresight = 0;
    public int Critical = 0;
    public int Dodge = 0;
    public enum EquipmentType {Weapon, Head, Body, Shield, Accessory,}
    public EquipmentType equipmentType;
    public enum WeaponType {Sword, DualSword, Knife, Staff, Hammer, Axe};
    public WeaponType weaponType;
    public int sellPrice = 0;
    public int buyPrice = 0;
    public string baseAssetName;
    public int enhancementLevel = 0;
    public Element element = Element.None;
    public List<MaterialAmount> smeltYield = new List<MaterialAmount>();
    public List<Effect> effects = new List<Effect>();
}
