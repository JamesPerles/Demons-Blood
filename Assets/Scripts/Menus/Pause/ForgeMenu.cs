using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using System;
public class ForgeMenu : MonoBehaviour, ICardHighlightHandler, ITabVisualOwner, IPageableTab
{
public PauseMenu host;
public TextMeshProUGUI forgeGoldText;
public TextMeshProUGUI forgeFeedbackText;
public GameObject craftCardPrefab;
public Transform craftCardParent;
public Button craftActionButton;
public TextMeshProUGUI craftActionButtonLabel;
public Color cardBorderDefault = new Color32(0x3A, 0x16, 0x16, 0xFF);
public Color cardBorderSelected = new Color32(0xD8, 0x5A, 0x30, 0xFF);
public Color cardBackgroundSelected = new Color32(0x24, 0x10, 0x10, 0xFF);
public Color cardTitleDefault  = new Color32(0xC9, 0xC2, 0xC2, 0xFF);
public Color cardTitleSelected = new Color32(0xF2, 0xF2, 0xF2, 0xFF);
const string colorMuted = "#8A8580";
const string colorBright = "#E8E4E0";
const string colorDone = "#639922";
const string dot ="\u00B7";
public int enhanceBaseCost = 100;
public int enhanceCostPerLevel = 50;
public int enhanceStrengthGain = 2;
public int maxEnhancementLevel = 10;
public int addElementCost = 200;
public List<CraftRecipe> craftRecipes = new List<CraftRecipe>();
public List<AlchemyRecipe> alchemyRecipes = new List<AlchemyRecipe>();
GridCardPager pager;
CraftRecipe selectedRecipe;
Equipment selectedEnhanceTarget;
ActiveStats selectedEnhanceOwner;
Equipment selectedWeaponForElement;
Equipment selectedSmeltTarget;
Baggable selectedFirstItem;
bool alchemyStage2;
bool elementStage2;
public void OpenTab()
    {
        host.PrepareTabSwitch();
        if(pager == null) pager = new GridCardPager(craftCardPrefab, craftCardParent, host, 3, 3);
        if(forgeFeedbackText != null) forgeFeedbackText.text = "";
        UpdateForgeGoldText();
        SetupMiniTabs();
    }
    public void HideVisuals()
    {
        if(craftCardParent != null) craftCardParent.gameObject.SetActive(false);
        if(craftActionButton != null) craftActionButton.gameObject.SetActive(false);
    }
    void SetupMiniTabs()
    {
        if(host.miniTabGroup == null) return;
        host.miniTabGroup.Show();
        List<TabDefinition> tabs = new List<TabDefinition>
        {
            new TabDefinition("Craft", ShowCraftTab),
            new TabDefinition("Alchemy", ShowAlchemyTab),
            new TabDefinition("Enhance", ShowEnhanceTab),
            new TabDefinition("Element", ShowElementTab),
            new TabDefinition("Smelt", ShowSmeltTab)
        };
        host.miniTabGroup.SetTabs(tabs, 0);
    }
    void SetActionButton(string label, bool enabled, Action onClick)
    {
        if(craftActionButton == null) return;
        craftActionButton.gameObject.SetActive(true);
        craftActionButton.interactable = enabled;
        if(craftActionButtonLabel != null) craftActionButtonLabel.text = label;
        craftActionButton.onClick.RemoveAllListeners();
        if(onClick != null) craftActionButton.onClick.AddListener(() => onClick());
    }
    void HideActionButton()
    {
        if(craftActionButton != null) craftActionButton.gameObject.SetActive(false);
    }
    void OpenCardTab(string breadcrumbSuffix)
    {
        if(craftCardParent != null) craftCardParent.gameObject.SetActive(true);
        host.ShowSplitPanel();
        host.SetBreadcrumbSuffix(breadcrumbSuffix);
        host.SetCardHighlightHandler(this);
        host.SetPageableTab(this);
        UpdateForgeGoldText();
    }
    public void NextPage()
    {
        pager?.NextPage();
        pager?.SelectFirstOnPage();
    }
    public void PreviousPage()
    {
        pager?.PreviousPage();
        pager?.SelectFirstOnPage();
    }
    void ShowCraftTab()
    {
        OpenCardTab("Forge > Craft");
        RebuildCraftCards();
    }
    void RebuildCraftCards()
    {
        if(pager == null || craftCardPrefab == null || craftCardParent == null) return;
        List<CardGridSpec> specs = new List<CardGridSpec>();
        foreach(CraftRecipe recipe in craftRecipes)
        {
            if(recipe == null) continue;
            if(recipe.result == null && recipe.itemResult == null) continue;
            string resultName = recipe.result != null ? recipe.result.equipmentName : recipe.itemResult.itemName;
            bool hasMaterials = HasMaterials(recipe);
            CraftRecipe captured = recipe;
            specs.Add(new CardGridSpec(resultName, hasMaterials ? "" : "missing materials", BuildCraftDetail(captured), () => SelectCraftRecipe(captured)));
        }
            pager.SetSpecs(specs);
            if(pager.SpawnedCards.Count > 0) pager.SelectFirstOnPage();
            else
            {
                HideActionButton();
                if(host.detailText != null) host.detailText.text = "No known recipes.";
            }
        }
        void SelectCraftRecipe(CraftRecipe recipe)
        {
            selectedRecipe = recipe;
            bool canCraft = HasMaterials(recipe) && (recipe.goldCost <= 0 || (WalletManager.instance != null && WalletManager.instance.currentGold >= recipe.goldCost));
            SetActionButton("Craft", canCraft, CraftSelectedRecipe);
        }
        string BuildCraftDetail(CraftRecipe recipe)
        {
        string resultName = recipe.result != null ? recipe.result.equipmentName : recipe.itemResult.itemName;
        string subtitle = recipe.result != null
        ? $"weapon/equipment {dot} {BuildEquipmentStatLine(recipe.result)}"
        : "item";
        string materials = "";
        foreach(MaterialAmount material in recipe.requiredMaterials)
        {
            if(material == null || material.material == null) continue;
            int owned = InventoryManager.instance.items.FindAll(i => i == material.material).Count;
            bool satisfied = owned >= material.amount;
            string color = satisfied ? colorDone : colorMuted;
            materials += $"{material.material.itemName} <color={color}>{Mathf.Min(owned, material.amount)} / {material.amount}</color>\n";
        }
        string result = $"<size=140%><color=#F2F2F2>{resultName}</color></size>\n";
        result += $"<size=80%><color={colorMuted}>{subtitle}</color></size>\n";
        result += $"<size=75%><color={colorMuted}>MATERIALS REQUIRED</color></size>\n";
        result += $"<color={colorBright}>{materials}</color>\n\n";
        result += $"<size=80%><color={colorMuted}>cost: {recipe.goldCost} gold</color></size>";
        return result;
    }
    string BuildEquipmentStatLine(Equipment equipment)
    {
        List<string> parts = new List<string>();
        if(equipment.strength != 0) parts.Add($"STR {(equipment.strength > 0 ? "+" : "")}{equipment.strength}");
        if(equipment.magic != 0) parts.Add($"MAG {(equipment.magic > 0 ? "+" : "")}{equipment.magic}");
        if(equipment.defense != 0) parts.Add($"DEF {(equipment.defense > 0 ? "+" : "")}{equipment.defense}");
        if(equipment.wisdom != 0) parts.Add($"WIS {(equipment.wisdom > 0 ? "+" : "")}{equipment.wisdom}");
        if(equipment.tech != 0) parts.Add($"TEC {(equipment.tech > 0 ? "+" : "")}{equipment.tech}");
        if(equipment.affinity != 0) parts.Add($"AFF {(equipment.affinity > 0 ? "+" : "")}{equipment.affinity}");
        if(equipment.speed != 0) parts.Add($"SPD {(equipment.speed > 0 ? "+" : "")}{equipment.speed}");
        if(equipment.luck != 0) parts.Add($"LCK {(equipment.luck > 0 ? "+" : "")}{equipment.luck}");
        return parts.Count > 0 ? string.Join(", ", parts) : "no stat bonus";
    }
    bool HasMaterials(CraftRecipe recipe)
    {
        foreach(MaterialAmount required in recipe.requiredMaterials)
        {
            int owned = InventoryManager.instance.items.FindAll(item => item == required.material).Count;
            if(owned < required.amount) return false;
        }
        return true;
    }
    void CraftSelectedRecipe()
    {
        CraftRecipe recipe = selectedRecipe;
        if(recipe == null) return;
        if(!HasMaterials(recipe)) { if(forgeFeedbackText != null) forgeFeedbackText.text = "Missing materials"; return;}
        if(recipe.goldCost > 0 && !TrySpendForge(recipe.goldCost)) return;
        foreach(MaterialAmount required in recipe.requiredMaterials)
        for(int i = 0; i < required.amount; i++) InventoryManager.instance.LoseItem(required.material);
        string craftedName;
        if(recipe.result != null)
        {
            Equipment crafted = Instantiate(recipe.result);
            InventoryManager.instance.PickupItem(crafted);
            craftedName = crafted.equipmentName;
        }
        else
        {
            InventoryManager.instance.PickupItem(recipe.itemResult);
            craftedName = recipe.itemResult.itemName;
        }
        if(forgeFeedbackText != null) forgeFeedbackText.text = $"Forged {craftedName}";
        RebuildCraftCards();
    }
    public void OnCardHighlighted(GameObject entry)
    {
        if(pager == null) return;
        for(int i = 0; i < pager.SpawnedCards.Count; i++)
        {
            if(pager.SpawnedCards[i] == null) continue;
            QuestCardView view = pager.SpawnedCards[i].GetComponent<QuestCardView>();
            if(view == null) continue;
            SetCardVisual(view, pager.SpawnedCards[i] == entry);
        }
    }
    void SetCardVisual(QuestCardView view, bool selected)
    {
        if(view.borderImage != null) view.borderImage.color = selected ? cardBorderSelected : cardBorderDefault;
        if(view.backgroundImage != null)
        {
            Color bg = cardBackgroundSelected;
            bg.a = selected ? 1f : 0f;
            view.backgroundImage.color = bg;
        }
        if(view.titleText != null) view.titleText.color = selected ? cardTitleSelected : cardTitleDefault;
    }
     void UpdateForgeGoldText()
    {
        if(forgeGoldText != null && WalletManager.instance != null) forgeGoldText.text = $"{WalletManager.instance.currentGold} Gold"; 
    }
    bool TrySpendForge(int cost)
    {
        if(WalletManager.instance == null || !WalletManager.instance.SpendGold(cost))
        {
            if(forgeFeedbackText != null) forgeFeedbackText.text = "Not enough gold.";
            return false;
        }
        UpdateForgeGoldText();
        return true;
    }
    void ShowEnhanceTab()
    {
        OpenCardTab("Forge > Enhance");
        RebuildEnhanceCards();
    }
    void RebuildEnhanceCards()
    {
        if(pager == null || craftCardPrefab == null || craftCardParent == null) return;
        List<CardGridSpec> specs = new List<CardGridSpec>();
        foreach(Equipment equipment in InventoryManager.instance.items.OfType<Equipment>())
        {
            if(equipment.equipmentType != Equipment.EquipmentType.Weapon) continue;
            Equipment captured = equipment;
            bool atMax = captured.enhancementLevel >= maxEnhancementLevel;
            string sub = atMax ? "Max Level" : $"+{captured.enhancementLevel} {dot} {EnhanceCost(captured.enhancementLevel)}g";
            specs.Add(new CardGridSpec(captured.equipmentName, sub, BuildEnhanceDetail(captured, null), () => SelectEnhanceTarget(captured, null)));
        }
        if(PlayerParty.instance != null)
        {
         foreach(GameObject characterObject in PlayerParty.instance.playableCharacters)
    {
        ActiveStats character = characterObject.GetComponent<ActiveStats>();
        if(character == null || character.weaponSlot == null) continue;
        Equipment equipped = character.weaponSlot;
        ActiveStats capturedOwner = character;
        Equipment capturedEquipped = equipped;
        bool atMax = equipped.enhancementLevel >= maxEnhancementLevel;
        string sub = atMax 
        ? $"Equipped: {character.currentName} {dot} MAX"
        : $"Equipped: {character.currentName} {dot} +{equipped.enhancementLevel} {dot} {EnhanceCost(equipped.enhancementLevel)}g";
        specs.Add(new CardGridSpec(equipped.equipmentName, sub, BuildEnhanceDetail(capturedEquipped, capturedOwner), () => SelectEnhanceTarget(capturedEquipped, capturedOwner)));
    }
   }
    pager.SetSpecs(specs);
    if(pager.SpawnedCards.Count > 0) pager.SelectFirstOnPage();
        else
        {
            HideActionButton();
            if(host.detailText != null) host.detailText.text = "No weapons to enhance";
        }
    }
    void SelectEnhanceTarget(Equipment equipment, ActiveStats owner)
    {
        selectedEnhanceTarget = equipment;
        selectedEnhanceOwner = owner;
        bool atMax = equipment.enhancementLevel >= maxEnhancementLevel;
        int cost = EnhanceCost(equipment.enhancementLevel);
        bool canAfford = WalletManager.instance != null && WalletManager.instance.currentGold >= cost;
        SetActionButton("Enhance", !atMax && canAfford, () => EnhanceWeapon(selectedEnhanceTarget, selectedEnhanceOwner));
    }
    string BuildEnhanceDetail(Equipment equipment, ActiveStats owner)
    {
        bool atMax = equipment.enhancementLevel >= maxEnhancementLevel;
        string subtitle = owner != null ? $"equipped by {owner.currentName}" : "in inventory";
        string result = $"<size=140%><color=#F2F2F2>{equipment.equipmentName}</color></size>\n";
        result += $"<size=80%><color={colorMuted}>{subtitle}</color></size>\n\n";
        if(atMax)
        {
           result += $"<color={colorMuted}>Already at maximum enhancement (+{maxEnhancementLevel}).</color>";
        }
        else
        {
            int cost = EnhanceCost(equipment.enhancementLevel);
            result += $"<size=75%><color={colorMuted}>ENHANCEMENT</color></size>\n";
            result += $"<color={colorBright}>+{equipment.enhancementLevel} -> +{equipment.enhancementLevel + 1}</color>\n";
            result += $"<color={colorBright}>STR {equipment.strength} -> +{equipment.strength + enhanceStrengthGain}</color>\n";
            result += $"<size=80%><color={colorMuted}>cost: {cost} gold</color></size>";
        }
        return result;
    }
    int EnhanceCost(int currentLevel) => enhanceBaseCost + (currentLevel * enhanceCostPerLevel);
    void EnhanceWeapon(Equipment original, ActiveStats owner = null)
    {
        int cost = EnhanceCost(original.enhancementLevel);
        if (!TrySpendForge(cost)) return;
        Equipment enhanced = Instantiate(original);
        enhanced.baseAssetName = string.IsNullOrEmpty(original.baseAssetName) ? original.name : original.baseAssetName;
        enhanced.strength += enhanceStrengthGain;
        enhanced.enhancementLevel = original.enhancementLevel + 1;
        enhanced.equipmentName = StripSuffix(original.equipmentName) + $" +{enhanced.enhancementLevel}";
        if(owner != null)
        {
            owner.personalInventory.RemoveItem(original);
            owner.personalInventory.AddItem(enhanced);
            owner.Equip(enhanced);
        }
        else
         {
        InventoryManager.instance.LoseItem(original);
        InventoryManager.instance.PickupItem(enhanced); 
        }
        if(forgeFeedbackText != null) forgeFeedbackText.text = $"Enhanced to {enhanced.equipmentName}.";
        RebuildEnhanceCards();
    }
    string StripSuffix(string name)
    {
        int plusIndex = name.LastIndexOf(" +");
        return plusIndex >= 0 ? name.Substring(0, plusIndex) : name;
    }
    void ShowElementTab()
    {
        OpenCardTab("Forge > Element");
        elementStage2 = false;
        RebuildElementCards();
    }
    void RebuildElementCards()
    {
        if(elementStage2) {RebuildElementPickCards(); return; }
        if(pager == null || craftCardPrefab == null || craftCardParent == null) return;
        List<CardGridSpec> specs = new List<CardGridSpec>();
        foreach(Equipment equipment in InventoryManager.instance.items.OfType<Equipment>())
        {
            if(equipment.equipmentType != Equipment.EquipmentType.Weapon) continue;
            if(equipment.element != Element.None) continue;
            Equipment captured = equipment;
            specs.Add(new CardGridSpec(captured.equipmentName, $"{addElementCost}g", BuildElementDetail(captured), () => SelectElementTarget(captured)));
        }
        pager.SetSpecs(specs);
        if(pager.SpawnedCards.Count > 0) pager.SelectFirstOnPage();
        else
        {
            HideActionButton();
            if(host.detailText != null) host.detailText.text = "No unenchanted weapons";
        }
    }
    void SelectElementTarget(Equipment weapon)
    {
        selectedWeaponForElement = weapon;
        bool canAfford = WalletManager.instance != null && WalletManager.instance.currentGold >= addElementCost;
        SetActionButton("Choose Element", canAfford, () => {elementStage2 = true; RebuildElementCards(); });
    }
    string BuildElementDetail(Equipment equipment)
    {
        string result = $"<size=140%><color=#F2F2F2>{equipment.equipmentName}</color></size>\n";
        result += $"<size=80%><color={colorMuted}>weapon {dot} no affinity yet</color></size>\n";
        result += $"<size=75%><color={colorMuted}>ENHANCE COST</color></size>\n<color={colorBright}>{addElementCost} gold</color>";
        return result;
    }
    void RebuildElementPickCards()
    {
        if(pager == null || craftCardPrefab == null || craftCardParent == null) return;
        List<CardGridSpec> specs = new List<CardGridSpec>();
        specs.Add(new CardGridSpec("Cancel", "", "Return to weapon selection.", () => {elementStage2 = false; RebuildElementCards(); }));
        foreach(Element element in Enum.GetValues(typeof(Element)))
        {
            if(element == Element.None) continue;
            Element captured = element;
            string detail = $"<size=140%><color=#F2F2F2>{captured}</color></size>\n<size=80%><color={colorMuted}>imbue{selectedWeaponForElement?.equipmentName} with {captured} affinity</color></size>";
            specs.Add(new CardGridSpec(captured.ToString(), "", detail, () => AddElement(captured)));
        }
        pager.SetSpecs(specs);
        HideActionButton();
        if(host.detailText != null)
        host.detailText.text = $"<size=140%><color=#F2F2F2>Choose an element</color></size>\n<size=80%><color={colorMuted}>for {selectedWeaponForElement?.equipmentName}</color></size>";
    }
    void AddElement(Element element)
    {
        Equipment original = selectedWeaponForElement;
        if(!TrySpendForge(addElementCost)) return;
        Equipment enhanced = Instantiate(original);
        enhanced.baseAssetName = string.IsNullOrEmpty(original.baseAssetName) ? original.name : original.baseAssetName;
        enhanced.element = element;
        enhanced.equipmentName = $"{original.equipmentName} ({element})";
        InventoryManager.instance.LoseItem(original);
        InventoryManager.instance.PickupItem(enhanced);
        if(forgeFeedbackText != null) forgeFeedbackText.text = $"{enhanced.equipmentName} imbued with {element}";
        elementStage2 = false;
        RebuildElementCards();
    }
    void ShowSmeltTab()
    {
        OpenCardTab("Forge > Smelt");
        RebuildSmeltCards();
    }
    void RebuildSmeltCards()
    {
        if(pager == null || craftCardPrefab == null || craftCardParent == null) return;
        List<CardGridSpec> specs = new List<CardGridSpec>();
        foreach(Equipment equipment in InventoryManager.instance.items.OfType<Equipment>())
        {
            if(equipment.smeltYield == null || equipment.smeltYield.Count == 0) continue;
            Equipment captured = equipment;
            specs.Add(new CardGridSpec(captured.equipmentName, "", BuildSmeltDetail(captured), () => SelectSmeltTarget(captured)));
        }
        pager.SetSpecs(specs);
        if(pager.SpawnedCards.Count > 0) pager.SelectFirstOnPage();
        else
        {
            HideActionButton();
            if(host.detailText != null) host.detailText.text = "Nothing smeltable.";
        }
    }
    void SelectSmeltTarget(Equipment equipment)
    {
        selectedSmeltTarget = equipment;
        SetActionButton("Smelt", true, () => SmeltWeapons(selectedSmeltTarget));
    }
    string BuildSmeltDetail(Equipment equipment)
    {
        string yieldText = string.Join(", ", equipment.smeltYield.Select(material => $"{material.amount}x {material.material.itemName}"));
        string result = $"<size=140%><color=#F2F2F2>{equipment.equipmentName}</color></size>\n";
        result += $"<size=80%><color={colorMuted}>break down for materials</color></size>\n\n";
        result += $"<size=75%><color={colorMuted}>YIELDS</color></size>\n<color={colorBright}>{yieldText}</color>";
        return result;
    }
    void SmeltWeapons(Equipment equipment)
    {
        InventoryManager.instance.LoseItem(equipment);
        foreach (MaterialAmount material in equipment.smeltYield)
        {
            for(int i = 0; i < material.amount; i++) InventoryManager.instance.PickupItem(material.material);
        }
        if(forgeFeedbackText != null) forgeFeedbackText.text = $"Smelted {equipment.equipmentName}.";
        RebuildSmeltCards();
    }
    void ShowAlchemyTab()
    {
        OpenCardTab("Forge > Alchemy");
        alchemyStage2 = false;
        RebuildAlchemyCards();
    }
    List<(Baggable item, int count)> BuildAlchemyGroupedList()
    {
        List<(Baggable, int)> result = new List<(Baggable, int)>();
        if(InventoryManager.instance == null) return result;
        Dictionary<Baggable, int> counts = new Dictionary<Baggable, int>();
        List<Baggable> order = new List<Baggable>();
        foreach(Baggable item in InventoryManager.instance.items)
        {
            if(item == null) continue;
            if(!counts.ContainsKey(item)) { counts[item] = 0; order.Add(item); }
            counts[item]++;
        }
        foreach(Baggable item in order) result.Add((item, counts[item]));
        return result;
    }
   void RebuildAlchemyCards()
    {
        if(alchemyStage2) {RebuildAlchemySecondCards(); return; }
        if(pager == null || craftCardPrefab == null || craftCardParent == null) return;
        List<(Baggable item, int count)> grouped = BuildAlchemyGroupedList();
        List<CardGridSpec> specs = new List<CardGridSpec>();
        foreach(var entry in grouped)
        {
            Baggable captured = entry.item;
            int capturedCount = entry.count;
            specs.Add(new CardGridSpec(captured.DisplayName, $"x{capturedCount}", BuildAlchemyItemDetail(captured, capturedCount), () => SelectAlchemyFirst(captured)));
        }
        pager.SetSpecs(specs);
        if(pager.SpawnedCards.Count > 0) pager.SelectFirstOnPage();
        else
        {
            HideActionButton();
            if(host.detailText != null) host.detailText.text = "Nothing to combine.";
        }
    }
    void SelectAlchemyFirst(Baggable item)
    {
        selectedFirstItem = item;
        SetActionButton("Choose Partner", true, () => {alchemyStage2 = true; RebuildAlchemyCards(); });
    }
    string BuildAlchemyItemDetail(Baggable item, int count)
    {
        string result = $"<size=140%><color=#F2F2F2>{item.DisplayName}</color></size>\n";
        result += $"<size=80%><color={colorMuted}>owned x{count} {dot} select as the first ingredient</color></size>";
        return result;
    }
    void RebuildAlchemySecondCards()
    {
        if(pager == null || craftCardPrefab == null || craftCardParent == null) return;
        List<CardGridSpec> specs = new List<CardGridSpec>();
        specs.Add(new CardGridSpec("Cancel", "", "Return to ingredient selection.", () => {alchemyStage2 = false; RebuildAlchemyCards(); }));
        List<(Baggable item, int count)> grouped = BuildAlchemyGroupedList();
        foreach (var entry in grouped)
        {
            Baggable captured = entry.item;
            int availableCount = entry.count;
            if(captured == selectedFirstItem) availableCount--;
            if(availableCount <= 0) continue;
            string detail = $"<size=140%><color=#F2F2F2>{selectedFirstItem?.DisplayName} + {captured.DisplayName}</color></size>\n<size=80%><color={colorMuted}>owned x {availableCount} {dot} combine these two?</color></size>";
            specs.Add(new CardGridSpec(captured.DisplayName, $"x{availableCount}", detail, () => CombineItems(captured)));
        }
        pager.SetSpecs(specs);
        HideActionButton();
        if(host.detailText != null)
        host.detailText.text = $"<size=140%><color=#F2F2F2>Combine with...</color></size>\n<size=80%><color={colorMuted}>what pairs with {selectedFirstItem?.DisplayName}?</color></size>";
    }
     void CombineItems(Baggable second)
    {
        Baggable first = selectedFirstItem;
        AlchemyRecipe matched = alchemyRecipes.Find(recipe => recipe != null &&
        ((recipe.ingredientA == first && recipe.ingredientB == second) ||
        (recipe.ingredientA == second && recipe.ingredientB == first)));
        if(matched == null || matched.result == null)
        {
            if(forgeFeedbackText != null) forgeFeedbackText.text = $"{first.DisplayName} + {second.DisplayName} does nothing.";
            alchemyStage2 = false;
            RebuildAlchemyCards();
            return;
        }
        InventoryManager.instance.LoseItem(first);
        InventoryManager.instance.LoseItem(second);
        if(matched.result is Item resultItem) InventoryManager.instance.PickupItem(resultItem);
        else if(matched.result is Equipment resultEquipment) InventoryManager.instance.PickupItem(Instantiate(resultEquipment));
        if(forgeFeedbackText != null) forgeFeedbackText.text = $"{first.DisplayName} + {second.DisplayName} became {matched.result.DisplayName}.";
    alchemyStage2 = false;
    RebuildAlchemyCards();
    }
}
