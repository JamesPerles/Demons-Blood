using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public Transform itemContent;
    public GameObject inventoryItem;
    public List<Item> items = new List<Item>();
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("Inventory Manager exists across scenes");
        }
        else
        {
            Destroy(gameObject);
            Debug.Log("Destroy extra Inventories");
        }
    }
    public void PickupItem(Item item)
    {
        items.Add(item);
    }
    public void LoseItem(Item item)
    {
        bool removed = items.Remove(item);
        if (!removed) Debug.LogWarning($"Tried to remove {item.itemName} but is not in inventory");
    }
    public void ListItems()
    {
        foreach (Transform item in itemContent) Destroy(item.gameObject);
        foreach (var item in items)
        {
            GameObject obj = Instantiate(inventoryItem, itemContent);
            var itemName = obj.GetComponentInChildren<TextMeshProUGUI>();
            if (itemName != null) itemName.text = item.itemName;
        }
    }
}
