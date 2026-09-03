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
public Color statIncreaseColor = new Color(0.30f, 0.69f, 0.31f);
public Color statDecreaseColor = new Color(0.90f, 0.22f, 0.21f);
Baggable pendingPurchase;
int pendingPrice;
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
        if (goldText != null && WalletManager.instance != null) goldText.text = $"{WalletManager.instance.currentGold} Gold";
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
            MenuOption option =  new MenuOption($"{captured.itemName} - {captured.buyPrice}g",() => ChooseBuy(captured, captured.buyPrice));
            option.description = captured.description;
            options.Add(option);      
        }
        foreach (Equipment equipment in currentShop.equipmentForSale)
        {
            Equipment captured = equipment;
            MenuOption option = new MenuOption($"{captured.equipmentName} - {captured.buyPrice}g",() => ChooseBuy(captured, captured.buyPrice));
            option.description = BuildEquipmentDescription(captured);
            options.Add(option);
        }
        return options;
    }
    void ChooseBuy(Baggable template, int price)
    {
        if(WalletManager.instance == null || WalletManager.instance.currentGold < price)
        {
            if(feedbackText != null) feedbackText.text = "Not enough gold";
            return;
        }
        pendingPurchase = template;
        pendingPrice = price;
        OpenScreen(DestinationMenu(), $"Buy{template.DisplayName}");
    }
     List<MenuOption> SellMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach (Item item in InventoryManager.instance.items.OfType<Item>().Where(i => i.itemType == Item.ItemType.Consumable))
        {
            Item captured = item;
            MenuOption option = new MenuOption($"{captured.itemName} - {captured.sellPrice}g", () => Sell(captured, captured.sellPrice));
            option.description = captured.description;
            options.Add(option);
        }
        foreach (Equipment equipment in InventoryManager.instance.items.OfType<Equipment>())
        {
            Equipment captured = equipment;
            MenuOption option = new MenuOption($"{captured.equipmentName} - {captured.sellPrice}g", () => Sell(captured,captured.sellPrice));
            option.description = BuildEquipmentDescription(captured);
            options.Add(option);
        }
        return options;
    }
    void Sell(Baggable owned, int price)
    {
        if(owned is Item ownedItem && ownedItem.itemType == Item.ItemType.KeyItem) return;
        InventoryManager.instance.LoseItem(owned);
        WalletManager.instance.AddGold(price);
        if(feedbackText != null) feedbackText.text = $"Sold{owned.DisplayName}!";
        UpdateGoldText();
        screenHistory.Pop();
        OpenScreen(SellMenu());
    }
    List<MenuOption> DestinationMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        options.Add(new MenuOption("Party Inventory", ConfirmPartyDestination));
        if(PlayerParty.instance != null)
        {
            foreach(GameObject characterObject in PlayerParty.instance.playableCharacters)
            {
                ActiveStats character = characterObject.GetComponent<ActiveStats>();
                if(character == null) continue;
                ActiveStats captured = character;
                bool full = !captured.personalInventory.CanAdd();
                string label = full ? $"{captured.currentName} (Full)" : $"{captured.currentName} ({captured.personalInventory.FreeSlots} free)";
           options.Add(new MenuOption(label, () => ChooseCharacterDestination(captured)));
            }
        }
        return options;
    }
    void ChooseCharacterDestination(ActiveStats character)
    {
        if(character.personalInventory.CanAdd()) FinalizeToPersonal(character);
        else OpenScreen(EvictMenu(character), $"{character.currentName}'s Inventory (Full)");
    }
    List<MenuOption> EvictMenu(ActiveStats character)
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach(Baggable carried in character.personalInventory.items)
        {
            Baggable captured = carried;
            options.Add(new MenuOption(captured.DisplayName, () => EvictAndRetry(character, captured)));
        }
        if(options.Count == 0) options.Add(new MenuOption("Nothing to send back", () => { }) { enabled = false});
        return options;
    }
    void EvictAndRetry(ActiveStats character, Baggable itemToEvict)
    {
        if(InventoryManager.instance != null) InventoryManager.instance.RemovePersonalInventory(itemToEvict, character);
       FinalizeToPersonal(character);
    }
    Baggable CreatePurchaseInstance()
    {
        if(pendingPurchase is Equipment equipmentTemplate) return Instantiate(equipmentTemplate);
        return pendingPurchase;
    }
    void ConfirmPartyDestination()
    {
        if(!SpendPendingGold()) return;
        Baggable purchased = CreatePurchaseInstance();
        if(purchased is Item item) InventoryManager.instance.PickupItem(item);
        else if(purchased is Equipment equipment) InventoryManager.instance.PickupItem(equipment);
        ShowBoughtFeedback(purchased.DisplayName);
        ReturnToFreshBuyMenu();
    }
    void FinalizeToPersonal(ActiveStats character)
    {
      if(!SpendPendingGold()) return;
      Baggable purchased = CreatePurchaseInstance();
      character.personalInventory.AddItem(purchased);
      ShowBoughtFeedback(purchased.DisplayName);
      ReturnToFreshBuyMenu();
    }
    bool SpendPendingGold()
    {
        if(WalletManager.instance == null || !WalletManager.instance.SpendGold(pendingPrice))
        {
            if(feedbackText != null) feedbackText.text = "Not enough gold";
            ReturnToFreshBuyMenu();
            return false;
        }
        UpdateGoldText();
        return true;
    }
    void ShowBoughtFeedback(string name)
    {
        if(feedbackText != null) feedbackText.text = $"Bought {name}";
    }
    void ReturnToFreshBuyMenu()
    {
        while(screenHistory.Count > 1) screenHistory.Pop();
        OpenScreen(BuyMenu());
    }
    string BuildEquipmentDescription(Equipment equipment)
    {
        List<string> lines = new List<string>();
        AppendStatLine(lines, "HP", equipment.hp);
        AppendStatLine(lines, "MP", equipment.mp);
        AppendStatLine(lines, "STR", equipment.strength);
        AppendStatLine(lines, "MAG", equipment.magic);
        AppendStatLine(lines, "DEF", equipment.defense);
        AppendStatLine(lines, "WIS", equipment.wisdom);
        AppendStatLine(lines, "TEC", equipment.tech);
        AppendStatLine(lines, "AFN", equipment.affinity);
        AppendStatLine(lines, "SPD", equipment.speed);
        AppendStatLine(lines, "LCK", equipment.luck);
        AppendStatLine(lines, "ACC", equipment.Accuracy);
        AppendStatLine(lines, "EVA", equipment.Evasion);
        AppendStatLine(lines, "PRE", equipment.Precision);
        AppendStatLine(lines, "FOR", equipment.Foresight);
        AppendStatLine(lines, "CRT", equipment.Critical);
        AppendStatLine(lines, "DGE", equipment.Dodge);
        if(lines.Count == 0) lines.Add("No stat changes");
        return string.Join("\n", lines);
    }
    void AppendStatLine(List<string> lines, string label, int value)
    {
        if(value == 0) return;
        Color color = value > 0 ? statIncreaseColor : statDecreaseColor;
        string hex = ColorUtility.ToHtmlStringRGB(color);
        string sign = value > 0 ? "+" : "";
        lines.Add($"<color=#{hex}>{sign}{value} {label}</color>");
    }
}
