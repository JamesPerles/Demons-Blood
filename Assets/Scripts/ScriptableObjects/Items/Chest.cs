using UnityEngine;
using System.Collections.Generic;
public class Chest : MonoBehaviour
{
public string flagKey;
public int goldReward = 0;
public Baggable reward;
public GameObject closedSprite;
public GameObject openSprite;
void Start()
    {
        RefreshSprite();
    }
    public bool IsOpened()
    {
        if(string.IsNullOrEmpty(flagKey))
        {
            Debug.LogWarning($"Chest on '{gameObject.name}' has no flagkey");
            return false;
        }
        return FlagManager.instance != null && FlagManager.instance.GetFlag(flagKey);
    }
    public void Interact()
    {
        if(IsOpened())
        {
            Debug.Log($"{gameObject.name} is empty");
            return;
        }
        if(goldReward > 0 && WalletManager.instance != null) WalletManager.instance.AddGold(goldReward);
        if(reward is Item item) InventoryManager.instance.PickupItem(item);
        else if(reward is Equipment equipment) InventoryManager.instance.PickupItem(Instantiate(equipment));
        FlagManager.instance.SetFlag(flagKey, true);
        RefreshSprite();
    }
    void RefreshSprite()
    {
        bool opened = IsOpened();
        if(closedSprite != null) closedSprite.SetActive(!opened);
        if(openSprite != null) openSprite.SetActive(opened);
    }
}
