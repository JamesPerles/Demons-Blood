using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;
    public GameObject inventoryItem;
    public List<Baggable> items = new List<Baggable>();
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("Inventory Manager exists across scenes");
        }
        else
        {
            Destroy(gameObject);
            Debug.Log("Destroy extra Inventories");
        }
    }
    public void PickupItem(Baggable item)
    {
        items.Add(item);
        if(QuestManager.instance != null) QuestManager.instance.ReportItemObtained(item);
    }
    public void LoseItem(Baggable item)
    {
        bool removed = items.Remove(item);
        if (!removed) Debug.LogWarning($"Tried to remove {item.DisplayName} but is not in inventory");
    }
    public bool AddPersonalInventory(Baggable item, ActiveStats target)
    {
        if(item == null || target == null) return false;
        if(!items.Contains(item)) return false;
        if(!target.personalInventory.CanAdd()) return false;
        items.Remove(item);
        target.personalInventory.AddItem(item);
        return true;
    }
    public bool RemovePersonalInventory(Baggable item, ActiveStats source)
    {
        if(item == null || source == null) return false;
        if(!source.personalInventory.Contains(item)) return false;
        if(item is Equipment equipment) source.UnequipRemovedEquipment(equipment);
        source.personalInventory.RemoveItem(item);
        items.Add(item);
        return true;
    }
}
