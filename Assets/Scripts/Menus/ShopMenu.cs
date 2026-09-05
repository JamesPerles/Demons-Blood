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
        OpenScreen(ShopOptionsMenu());
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
    List<MenuOption> ShopOptionsMenu()
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
            MenuOption option =  new MenuOption($"{captured.itemName} - {captured.buyPrice}g",() => Buy(captured, captured.buyPrice));
            option.description = captured.description;
            options.Add(option);      
        }
        foreach (Equipment equipment in currentShop.equipmentForSale)
        {
            Equipment captured = equipment;
            MenuOption option = new MenuOption($"{captured.equipmentName} - {captured.buyPrice}g",() => Buy(captured, captured.buyPrice));
            option.description = EquipmentDescription(captured);
            options.Add(option);
        }
        return options;
    }
    void Buy(Baggable template, int price)
    {
        if(WalletManager.instance == null || WalletManager.instance.currentGold < price)
        {
            if(feedbackText != null) feedbackText.text = "Not enough gold";
            return;
        }
        pendingPurchase = template;
        pendingPrice = price;
        OpenScreen(DestinationMenu(), $"Buy {template.DisplayName}");
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
            option.description = EquipmentDescription(captured);
            options.Add(option);
        }
        return options;
    }
    void Sell(Baggable owned, int price)
    {
        if(owned is Item ownedItem && ownedItem.itemType == Item.ItemType.KeyItem) return;
        InventoryManager.instance.LoseItem(owned);
        WalletManager.instance.AddGold(price);
        if(feedbackText != null) feedbackText.text = $"Sold {owned.DisplayName}!";
        UpdateGoldText();
        screenHistory.Pop();
        OpenScreen(SellMenu());
    }
    List<MenuOption> DestinationMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        options.Add(new MenuOption("Party Inventory", ConfirmDestination));
        if(PlayerParty.instance != null)
        {
            foreach(GameObject characterObject in PlayerParty.instance.playableCharacters)
            {
                ActiveStats character = characterObject.GetComponent<ActiveStats>();
                if(character == null) continue;
                ActiveStats captured = character;
                bool full = !captured.personalInventory.CanAdd();
                string label = full ? $"{captured.currentName} (Full)" : $"{captured.currentName} ({captured.personalInventory.FreeSlots} free)";
           options.Add(new MenuOption(label, () => ChooseDestination(captured)));
            }
        }
        return options;
    }
    void ChooseDestination(ActiveStats character)
    {
        if(character.personalInventory.CanAdd()) ToPersonal(character);
        else OpenScreen(RemoveMenu(character), $"{character.currentName}'s Inventory (Full)");
    }
    List<MenuOption> RemoveMenu(ActiveStats character)
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach(Baggable carried in character.personalInventory.items)
        {
            Baggable captured = carried;
            options.Add(new MenuOption(captured.DisplayName, () => RemoveAndRetry(character, captured)));
        }
        if(options.Count == 0) options.Add(new MenuOption("Nothing to send back", () => { }) { enabled = false});
        return options;
    }
    void RemoveAndRetry(ActiveStats character, Baggable itemToRemove)
    {
        if(InventoryManager.instance != null) InventoryManager.instance.RemovePersonalInventory(itemToRemove, character);
        ToPersonal(character);
    }
    Baggable Purchase()
    {
        if(pendingPurchase is Equipment equipmentTemplate) return Instantiate(equipmentTemplate);
        return pendingPurchase;
    }
    void ConfirmDestination()
    {
        if(!SpendGold()) return;
        Baggable purchased = Purchase();
        if(purchased is Item item) InventoryManager.instance.PickupItem(item);
        else if(purchased is Equipment equipment) InventoryManager.instance.PickupItem(equipment);
        ShowBoughtFeedback(purchased.DisplayName);
        ReturnToBuyMenu();
    }
    void ToPersonal(ActiveStats character)
    {
      if(!SpendGold()) return;
      Baggable purchased = Purchase();
      character.personalInventory.AddItem(purchased);
      ShowBoughtFeedback(purchased.DisplayName);
      ReturnToBuyMenu();
    }
    bool SpendGold()
    {
        if(WalletManager.instance == null || !WalletManager.instance.SpendGold(pendingPrice))
        {
            if(feedbackText != null) feedbackText.text = "Not enough gold";
            ReturnToBuyMenu();
            return false;
        }
        UpdateGoldText();
        return true;
    }
    void ShowBoughtFeedback(string name)
    {
        if(feedbackText != null) feedbackText.text = $"Bought {name}";
    }
    void ReturnToBuyMenu()
    {
        while(screenHistory.Count > 1) screenHistory.Pop();
        OpenScreen(BuyMenu());
    }
    string EquipmentDescription(Equipment equipment)
    {
        List<string> lines = new List<string>();
        ShowStatLine(lines, "HP", equipment.hp);
        ShowStatLine(lines, "MP", equipment.mp);
        ShowStatLine(lines, "STR", equipment.strength);
        ShowStatLine(lines, "MAG", equipment.magic);
        ShowStatLine(lines, "DEF", equipment.defense);
        ShowStatLine(lines, "WIS", equipment.wisdom);
        ShowStatLine(lines, "TEC", equipment.tech);
        ShowStatLine(lines, "AFN", equipment.affinity);
        ShowStatLine(lines, "SPD", equipment.speed);
        ShowStatLine(lines, "LCK", equipment.luck);
        ShowStatLine(lines, "ACC", equipment.Accuracy);
        ShowStatLine(lines, "EVA", equipment.Evasion);
        ShowStatLine(lines, "PRE", equipment.Precision);
        ShowStatLine(lines, "FOR", equipment.Foresight);
        ShowStatLine(lines, "CRT", equipment.Critical);
        ShowStatLine(lines, "DGE", equipment.Dodge);
        if(lines.Count == 0) lines.Add("No stat changes");
        return string.Join("\n", lines);
    }
    void ShowStatLine(List<string> lines, string label, int value)
    {
        if(value == 0) return;
        Color color = value > 0 ? statIncreaseColor : statDecreaseColor;
        string hex = ColorUtility.ToHtmlStringRGB(color);
        string sign = value > 0 ? "+" : "";
        lines.Add($"<color=#{hex}>{sign}{value} {label}</color>");
    }
}
