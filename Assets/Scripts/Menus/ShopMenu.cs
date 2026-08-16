using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
public class ShopMenu : SubMenu
{
public static ShopMenu instance;
public TextMeshProUGUI shopNameText;
public TextMeshProUGUI goldText;
public TextMeshProUGUI feedbackText;
ShopStats currentShop;
void Awake()
    {
        if(instance == null) instance = this; else Destroy(gameObject);
    }
    void Start()
    {
        SetDisplayActive(false);
    }
    public void Open(ShopStats shop)
    {
        currentShop = shop;
        SetDisplayActive(true);
        if(shopNameText != null) shopNameText.text = shop.shopName;
        if(feedbackText != null) feedbackText.text = "";
        UpdateGoldText();
        screenHistory.Clear();
        OpenScreen(MainShopMenu());
    }
    public override void Close()
    {
        SetDisplayActive(false);
        ClearEntries();
        screenHistory.Clear();
        currentShop = null;
    }
    void UpdateGoldText()
    {
        if (goldText != null && Wallet.instance != null) goldText.text = $"{Wallet.instance.currentGold} Gold";
    }
    List<MenuOption> MainShopMenu()
    {
        return new List<MenuOption>
        {
            new MenuOption("Buy", BuyMenu),
            new MenuOption("Sell", SellMenu),
            new MenuOption("Leave", (System.Action)Close)
        };
    }
    List<MenuOption> BuyMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach (Item item in currentShop.itemsForSale)
        {
            Item captured = item;
            options.Add(new MenuOption($"{captured.itemName} - {captured.buyPrice}g",() => BuyItem(captured)));
        }
        foreach (Equipment equipment in currentShop.equipmentForSale)
        {
            Equipment captured = equipment;
            options.Add(new MenuOption($"{captured.equipmentName} - {captured.buyPrice}g",() => BuyEquipment(captured)));
        }
        return options;
    }
     List<MenuOption> SellMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach (Item item in InventoryManager.Instance.items.OfType<Item>().Where(i => i.itemType == Item.ItemType.Consumable))
        {
            Item captured = item;
            options.Add(new MenuOption($"{captured.itemName} - {captured.sellPrice}g",() => SellItem(captured)));
        }
        foreach (Equipment equipment in InventoryManager.Instance.items.OfType<Equipment>())
        {
            Equipment captured = equipment;
            options.Add(new MenuOption($"{captured.equipmentName} - {captured.sellPrice}g",() => SellEquipment(captured)));
        }
        return options;
    }
    void BuyItem(Item item)
    {
        if(!Unaffordable(item.buyPrice)) return;
        InventoryManager.Instance.PickupItem(item);
        if(feedbackText != null) feedbackText.text = $"Bought {item.itemName}!";
        RefreshCurrentScreen();
    }
    void BuyEquipment(Equipment equipment)
    {
         if(!Unaffordable(equipment.buyPrice)) return;
        InventoryManager.Instance.PickupEquipment(Instantiate(equipment));
        if(feedbackText != null) feedbackText.text = $"Bought {equipment.equipmentName}!";
        RefreshCurrentScreen();
    }
    void SellItem(Item item)
    {
        if(item.itemType == Item.ItemType.KeyItem) return;
        InventoryManager.Instance.LoseItem(item);
        Wallet.instance.AddGold(item.sellPrice);
        if(feedbackText != null) feedbackText.text = $"Sold {item.itemName}!";
        UpdateGoldText();
        screenHistory.Pop();
        OpenScreen(SellMenu());
    }
    void SellEquipment(Equipment equipment)
    {
        InventoryManager.Instance.LoseItem(equipment);
        Wallet.instance.AddGold(equipment.sellPrice);
        if(feedbackText != null) feedbackText.text = $"Sold {equipment.equipmentName}!";
        UpdateGoldText();
        screenHistory.Pop();
        OpenScreen(SellMenu());
    }
    bool Unaffordable(int price)
    {
        if(Wallet.instance == null || !Wallet.instance.SpendGold(price))
        {
            if(feedbackText != null) feedbackText.text = "Not enough gold";
            return false;
        }
        UpdateGoldText();
        return true;
    }
    void RefreshCurrentScreen()
    {
        FillMenu(screenHistory.Peek());
    }
}
