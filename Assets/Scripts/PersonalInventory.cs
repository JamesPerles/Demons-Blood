using System.Collections.Generic;
[System.Serializable]
public class PersonalInventory
{
    public const int Capacity = 20;
    public List<Baggable> items = new List<Baggable>();
    public bool IsFull => items.Count >= Capacity;
    public int FreeSlots => Capacity - items.Count;
    public bool CanAdd() => items.Count < Capacity;
    public bool Contains(Baggable item) => item != null && items.Contains(item);
    public bool AddItem(Baggable item)
    {
        if(item == null || !CanAdd()) return false;
        items.Add(item);
        return true;
    }
    public bool RemoveItem(Baggable item)
    {
        return item != null && items.Remove(item);
    }
    public List<Equipment> GetEquippableOfType(Equipment.EquipmentType type, List<Equipment.WeaponType> allowedWeaponTypes = null)
    {
        List<Equipment> matches = new List<Equipment>();
        foreach(Baggable item in items)
        {
            if(!(item is Equipment equipment) || equipment.equipmentType != type) continue;
            if(type == Equipment.EquipmentType.Weapon && allowedWeaponTypes != null && !allowedWeaponTypes.Contains(equipment.weaponType)) continue;
            matches.Add(equipment);
        }
        return matches;
    }
    public void SortAlphabetical()
    {
        items.Sort((a, b) => string.Compare(a != null ? a.DisplayName : "", b != null ? b.DisplayName : ""));
    }
    public void SortByType()
    {
        items.Sort((a, b) =>
        {
            int typeA = a is Equipment ? 0 : 1;
            int typeB = b is Equipment ? 0 : 1;
            if(typeA != typeB) return typeA.CompareTo(typeB);
            return string.Compare(a != null ? a.DisplayName : "", b != null ? b.DisplayName : "");
        });
    }
}
