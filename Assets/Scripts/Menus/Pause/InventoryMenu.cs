using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
public class InventoryMenu : MonoBehaviour, ICardHighlightHandler, ITabHider, IPageableTab
{
public PauseMenu host;
 public GameObject itemCardPrefab;
 public Transform itemCardParent;
 public Button useButton;
 public TextMeshProUGUI useButtonLabel;
 public Button sendButton;
 public TextMeshProUGUI sendButtonLabel;
 public Button discardButton;
 public TextMeshProUGUI discardButtonLable;
public Color cardBorderDefault = new Color32(0x3A, 0x16, 0x16, 0xFF);
public Color cardBorderSelected = new Color32(0xD8, 0x5A, 0x30, 0xFF);
public Color cardBackgroundSelected = new Color32(0x24, 0x10, 0x10, 0xFF);
public Color cardTitleDefault  = new Color32(0xC9, 0xC2, 0xC2, 0xFF);
public Color cardTitleSelected = new Color32(0xF2, 0xF2, 0xF2, 0xFF);
const string colorMuted = "#8A8580";
const string colorBody = "#C9C2C2";
const string colorBright = "#E8E4E0";
const string dot = "\u00B7";
enum Category { Consumables, Equipment, Materials, KeyItems }
enum Mode { Category, ChooseUseTarget, ChooseSendTarget }
Category activeCategory = Category.Consumables;
Mode mode = Mode.Category;
GridCardPager pager;
Baggable selectedItem;
    public void OpenTab()
    {
        host.PrepareTabSwitch();
        if(pager == null) pager = new GridCardPager(itemCardPrefab, itemCardParent, host, 2, 3);
        SetupMiniTabs();
    }
    public void HideVisuals()
    {
        if(itemCardParent != null) itemCardParent.gameObject.SetActive(false);
        SetActionButtonsVisible(false);
    }
    void SetupMiniTabs()
    {
        if(host.miniTabGroup == null) return;
        host.miniTabGroup.Show();
        List<TabDefinition> tabs = new List<TabDefinition>
        {
            new TabDefinition("Items", () => ShowCategory(Category.Consumables)),
            new TabDefinition("Equipment", () => ShowCategory(Category.Equipment)),
            new TabDefinition("Key Items", () => ShowCategory(Category.KeyItems)),
              new TabDefinition("Materials", () => ShowCategory(Category.Materials)), 
        };
        host.miniTabGroup.SetTabs(tabs, 0);
    }
    void ShowCategory(Category category)
    {
        activeCategory = category;
        mode = Mode.Category;
        if(itemCardParent != null) itemCardParent.gameObject.SetActive(true);
        host.ShowSplitPanel();
        host.SetCardHighlightHandler(this);
        host.SetPageableTab(this);
        WireActionButtons();
        RebuildCards(0);
    }
    void WireActionButtons()
    {
        if(useButton != null)
        {
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(BeginUseFlow);
        }
        if(sendButton != null)
        {
            sendButton.onClick.RemoveAllListeners();
            sendButton.onClick.AddListener(BeginSendFlow);
        }
        if(discardButton != null)
        {
            discardButton.onClick.RemoveAllListeners();
            discardButton.onClick.AddListener(DiscardSelected);
        }
        if(sendButtonLabel != null) sendButtonLabel.text = "Send";
        if(discardButtonLable != null) discardButtonLable.text = "Discard";
    }
    List<(Baggable item, int count)> BuildGroupedList()
    {
        List<(Baggable, int)> result = new List<(Baggable, int)>();
        if(InventoryManager.instance == null) return result;
        if(activeCategory == Category.Equipment)
        {
            foreach(Equipment equipment in InventoryManager.instance.items.OfType<Equipment>())
            result.Add((equipment, 1));
            return result;
        }
        if(activeCategory == Category.Materials)
        {
            Dictionary<Materials, int> materialCounts = new Dictionary<Materials, int>();
            List<Materials> materialOrder = new List<Materials>();
            foreach(Materials material in InventoryManager.instance.items.OfType<Materials>())
            {
                if(!materialCounts.ContainsKey(material)) {materialCounts[material] = 0; materialOrder.Add(material);}
                materialCounts[material]++;
            }
            foreach(Materials material in materialOrder) result.Add((material, materialCounts[material]));
            return result;
        }
        Item.ItemType wantedType = activeCategory == Category.Consumables ? Item.ItemType.Consumable : Item.ItemType.KeyItem;
        Dictionary<Item, int> counts = new Dictionary<Item, int>();
        List<Item> order = new List<Item>();
        foreach(Item item in InventoryManager.instance.items.OfType<Item>())
        {
            if(item.itemType != wantedType) continue;
            if(!counts.ContainsKey(item)) {counts[item] = 0; order.Add(item); }
            counts[item]++;
        }
        foreach(Item item in order) result.Add((item, counts[item]));
        return result;
    }
    void RebuildCards(int page)
    {
        if(pager == null || itemCardPrefab == null || itemCardParent == null) return;
        if(mode != Mode.Category) {RebuildTargetCards(page); return; }
        List<(Baggable item, int count)> grouped = BuildGroupedList();
        List<CardGridSpec> specs = new List<CardGridSpec>();
        foreach(var entry in grouped)
        {
            Baggable capturedItem = entry.item;
            int capturedCount = entry.count;
            string sub = activeCategory == Category.Equipment ? "" : $"x{entry.count}";
            specs.Add(new CardGridSpec(entry.item.StyledName, sub, 
            BuildItemDetail(capturedItem, capturedCount), () =>
            {
                    selectedItem = capturedItem;
                    RefreshActionButtons();
                }));
        }
        pager.SetSpecs(specs, page);
        if(host.pageText != null && pager.MaxPage == 0) host.pageText.text = "";
        if(pager.SpawnedCards.Count > 0) pager.SelectFirstOnPage();
        else
        {
            selectedItem = null;
            if(host.detailText != null) host.detailText.text = "Nothing here";
        }
        RefreshActionButtons();
    }
    string BuildItemDetail(Baggable baggable, int count)
    {
        string title = baggable.DisplayName;
        string subtitle;
        string description;
        string extra = "";
        if(baggable is Item item)
        {
            subtitle = item.itemType == Item.ItemType.Consumable ? "consumable" : "key item";
            description = item.description;
            if(item.effects != null && item.effects.Count > 0)
            {
                List<string> names = new List<string>();
                foreach(var effect in item.effects) if(effect != null) names.Add(effect.name);
                extra = $"<size=75%><color={colorMuted}>EFFECT</color></size>\n<color=#639922>{string.Join(", ", names)}</color>\n\n";
            }
        }
        else if(baggable is Equipment equipment)
        {
            subtitle = $"{equipment.equipmentType} {dot} +{equipment.enhancementLevel}";
            description = "";
            extra = $"<size=75%><color={colorMuted}>STATS</color></size>\n<color={colorBright}>{BuildEquipmentStatLine(equipment)}</color>\n\n";
        }
        else if(baggable is Materials material)
        {
            subtitle = "material";
            description = material.description;
        }
        else
        {
            subtitle = "";
            description = "";
        }
        string result = $"<size=140%><color=#F2F2F2>{title}</color></size>\n";
        result += $"<size=80%><color={colorMuted}>{subtitle}</color></size>\n\n";
        if(!string.IsNullOrEmpty(description)) result += $"<color={colorBody}>{description}</color>\n\n";
        result += extra;
        return result;
    }
    string BuildEquipmentStatLine(Equipment equipment)
    {
        List<string> parts = new List<string>();
        if(equipment.hp != 0) parts.Add($"HP {(equipment.hp > 0 ? "+" : "")}{equipment.hp}");
        if(equipment.mp != 0) parts.Add($"MP {(equipment.mp > 0 ? "+" : "")}{equipment.mp}");
        if(equipment.strength != 0) parts.Add($"STR {(equipment.strength > 0 ? "+" : "")}{equipment.strength}");
        if(equipment.magic != 0) parts.Add($"MAG {(equipment.magic > 0 ? "+" : "")}{equipment.magic}");
        if(equipment.defense != 0) parts.Add($"DEF {(equipment.defense > 0 ? "+" : "")}{equipment.defense}");
        if(equipment.wisdom != 0) parts.Add($"WIS {(equipment.wisdom > 0 ? "+" : "")}{equipment.wisdom}");
        if(equipment.tech != 0) parts.Add($"TEC {(equipment.tech > 0 ? "+" : "")}{equipment.tech}");
        if(equipment.affinity != 0) parts.Add($"AFF {(equipment.affinity > 0 ? "+" : "")}{equipment.affinity}");
        if(equipment.luck != 0) parts.Add($"LCK {(equipment.luck > 0 ? "+" : "")}{equipment.luck}");
        if(equipment.speed != 0) parts.Add($"SPD {(equipment.speed > 0 ? "+" : "")}{equipment.speed}");
        return parts.Count > 0 ? string.Join(", ", parts) : "no stat bonus";
    }
    void RefreshActionButtons()
    {
        bool hasSelection = selectedItem != null;
        bool isKeyItem = selectedItem is Item i && i.itemType == Item.ItemType.KeyItem;
        bool isEquipment = selectedItem is Equipment;
        SetActionButtonsVisible(hasSelection);
        if(!hasSelection) return;
        if(useButton != null) useButton.interactable = !isKeyItem;
        if(useButtonLabel != null) useButtonLabel.text = isEquipment ? "Equip" : "Use";
        if(sendButton != null) sendButton.interactable = true;
        if(discardButton != null) discardButton.interactable = !isKeyItem && !(selectedItem is Equipment lockedPick && lockedPick.locked);
        if(host.detailText != null && mode == Mode.Category && !isKeyItem && !host.detailText.text.Contains("Choose party member"))
        host.detailText.text += $"\n<size=70%><color={colorMuted}>Choose party member</color></size>";
    }
    void SetActionButtonsVisible(bool visible)
    {
        if(useButton != null) useButton.gameObject.SetActive(visible);
        if(sendButton != null) sendButton.gameObject.SetActive(visible);
        if(discardButton != null) discardButton.gameObject.SetActive(visible);
    }
    public void NextPage()
    {
        RebuildCards(pager.CurrentPage + 1 > pager.MaxPage ? 0 : pager.CurrentPage + 1);
    }
    public void PreviousPage()
    {
        RebuildCards(pager.CurrentPage - 1 < 0 ? pager.MaxPage : pager.CurrentPage - 1);
    }
    void BeginUseFlow()
    {
        if(selectedItem == null) return;
        mode = Mode.ChooseUseTarget;
        RebuildCards(0);
    }
    void BeginSendFlow()
    {
        if(selectedItem == null) return;
        mode = Mode.ChooseSendTarget;
        RebuildCards(0);
    }
    void DiscardSelected()
    {
        if(selectedItem == null) return;
        if(selectedItem is Equipment lockedEquipment && lockedEquipment.locked) { if(PopupManager.instance != null) PopupManager.instance.Show($"{lockedEquipment.equipmentName} is locked and cannot be discarded."); return; }
        InventoryManager.instance.LoseItem(selectedItem);
        if(PopupManager.instance != null) PopupManager.instance.Show($"Discarded {selectedItem.DisplayName}");
        RebuildCards(0);
    }
    void RebuildTargetCards(int page = 0)
    {
        List<CardGridSpec> specs = new List<CardGridSpec>();
        specs.Add(new CardGridSpec("Cancel", "", "Return to the item list.", () => {mode = Mode.Category; RebuildCards(0); }));
        if(PlayerParty.instance != null)
        {
        foreach(GameObject characterObject in PlayerParty.instance.playableCharacters)
        {
            ActiveStats character = characterObject.GetComponent<ActiveStats>();
            if(character == null) continue;
            ActiveStats capturedCharacter = character;
            specs.Add(new CardGridSpec(character.currentName, $"HP {character.currentHP}/{character.finalHP}", $"HP {character.currentHP}/{character.finalHP}", () => ConfirmTarget(capturedCharacter)));
        }
    }
        pager.SetSpecs(specs, page);
        if(host.detailText != null)
        host.detailText.text = mode == Mode.ChooseUseTarget
        ? $"<size=140%><color=#F2F2F2>Choose a target</color></size>\n<size=80%><color={colorMuted}>who should receive {selectedItem?.DisplayName}?</color></size>"
        : $"<size=140%><color=#F2F2F2>Send to...</color></size>\n<size=80%><color={colorMuted}>who should carry {selectedItem?.DisplayName}?</color></size>";
        SetActionButtonsVisible(false);
    }
    void ConfirmTarget(ActiveStats target)
    {
        if(selectedItem == null) {mode = Mode.Category; RebuildCards(0); return; }
        if(mode == Mode.ChooseUseTarget)
        {
            if(selectedItem is Item item && item.itemType == Item.ItemType.Consumable)
            {
                if(item.effects != null)
                foreach(Effect effect in item.effects)
                if(effect != null) StartCoroutine(effect.Apply(target, target));
                InventoryManager.instance.LoseItem(item);
                if(PopupManager.instance != null) PopupManager.instance.Show($"Used {item.itemName} on {target.currentName}.");
            }
            else if(selectedItem is Equipment equipment)
            {
                if(equipment.equipmentType == Equipment.EquipmentType.Weapon && target.playerStats != null && 
                !target.playerStats.allowedWeaponTypes.Contains(equipment.weaponType)) { if(PopupManager.instance != null) 
                PopupManager.instance.Show($"{target.currentName} cannot use {equipment.weaponType}."); 
                mode = Mode.Category; RebuildCards(0); return; }
                string blocked = target.EquipBlockReason(equipment);
                if(blocked != null) { if(PopupManager.instance != null) PopupManager.instance.Show(blocked); mode = Mode.Category; RebuildCards(0); return; }
                if(!InventoryManager.instance.AddPersonalInventory(equipment, target)) 
                { if(PopupManager.instance != null) PopupManager.instance.Show($"{target.currentName}'s bag is full."); 
                mode = Mode.Category; RebuildCards(0); return; }
                target.Equip(equipment);
                 if(PopupManager.instance != null) PopupManager.instance.Show($"Equipped {equipment.equipmentName} on {target.currentName}.");
            }
        }
        else if(mode == Mode.ChooseSendTarget)
        {
            bool moved = InventoryManager.instance.AddPersonalInventory(selectedItem, target);
            if(PopupManager.instance != null)
            PopupManager.instance.Show(moved ? $"Sent {selectedItem.DisplayName} to {target.currentName}." : $"{target.currentName}'s bag is full.");
        }
        mode = Mode.Category;
        RebuildCards(0);
    }
    public void OnCardHighlighted(GameObject entry)
    {
        if(pager == null) return;
        for(int i = 0; i < pager.SpawnedCards.Count; i++)
        {
            if(pager.SpawnedCards[i] == null) continue;
            EntryCard card = pager.SpawnedCards[i].GetComponent<EntryCard>();
            if(card == null) continue;
            SetCardVisual(card, pager.SpawnedCards[i] == entry);
        }
    }
    void SetCardVisual(EntryCard card, bool selected)
    {
        if(card.borderImage != null) card.borderImage.color = selected ? cardBorderSelected : cardBorderDefault;
        if(card.backgroundImage != null)
        {
            Color bg = cardBackgroundSelected;
            bg.a = selected ? 1f : 0f;
            card.backgroundImage.color = bg;
        }
        if(card.titleText != null) card.titleText.color = (SettingsManager.instance != null ? (selected ?SettingsManager.instance.TextSelectedColor : SettingsManager.instance.TextDefaultColor) : (selected ? cardTitleSelected : cardTitleDefault));
    }
}
