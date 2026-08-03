using System.Collections.Generic;
using UnityEngine;
public class EquipmentManager : MonoBehaviour
{
public static EquipmentManager instance;
public List<Equipment> equipment = new List<Equipment>();
void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
    public void PickupEquipment(Equipment item)
    {
        equipment.Add(item);
    }
    public void LoseEquipment(Equipment item)
    {
        bool removed = equipment.Remove(item);
        if (!removed) Debug.LogWarning($"Removal of{item.equipmentName} failed");
    }
}
