using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
public class ForgeMenu : MonoBehaviour, ICardHighlightHandler, ITabVisualOwner
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
Equipment selectedWeaponForElement;
public List<CraftRecipe> craftRecipes = new List<CraftRecipe>();
public List<AlchemyRecipe> alchemyRecipes = new List<AlchemyRecipe>();
Baggable selectedFirstItem;
List<GameObject> spawnedCraftCards = new List<GameObject>();
CraftRecipe selectedRecipe;
public void OpenTab()
    {
        host.PrepareTabSwitch();
        if(forgeFeedbackText != null) forgeFeedbackText.text = "";
        UpdateForgeGoldText();
     //   SetupMiniTabs();
    }
    public void HideVisuals()
    {
        if(craftCardParent != null) craftCardParent.gameObject.SetActive(false);
        if(craftActionButton != null) craftActionButton.gameObject.SetActive(false);
    }
    //continue forge menu stuff
     void UpdateForgeGoldText()
    {
        if(forgeGoldText != null && Wallet.instance != null) forgeGoldText.text = $"{Wallet.instance.currentGold} Gold"; 
    }
    bool TrySpendForge(int cost)
    {
        if(Wallet.instance == null || !Wallet.instance.SpendGold(cost))
        {
            if(forgeFeedbackText != null) forgeFeedbackText.text = "Not enough gold.";
            return false;
        }
        UpdateForgeGoldText();
        return true;
    }
    List<MenuOption> EnhanceListMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach(Equipment equipment in InventoryManager.Instance.items.OfType<Equipment>())
        {
            if(equipment.equipmentType != Equipment.EquipmentType.Weapon) continue;
            Equipment captured = equipment;
            int cost = EnhanceCost(captured.enhancementLevel);
            bool atMax = captured.enhancementLevel >= maxEnhancementLevel;
            string label = atMax ? $"{captured.equipmentName} (MAX)" : $"{captured.equipmentName} - STR {captured.strength} -> {captured.strength + enhanceStrengthGain} ({cost}g)";
        MenuOption option = new MenuOption(label, () => EnhanceWeapon(captured));
        option.enabled = !atMax;
        options.Add(option);
        }
         foreach(GameObject characterObject in PlayerParty.instance.playableCharacters)
    {
        ActiveStats character = characterObject.GetComponent<ActiveStats>();
        if(character == null || character.weaponSlot == null) continue;
        Equipment equipped = character.weaponSlot;
        ActiveStats capturedOwner = character;
        int cost = EnhanceCost(equipped.enhancementLevel);
        bool atMax = equipped.enhancementLevel >= maxEnhancementLevel;
        string label = atMax 
        ? $"{equipped.equipmentName} (Equipped: {character.currentName}) (MAX)" : 
        $"{equipped.equipmentName} (Equipped: {character.currentName}) - STR {equipped.strength} -> {equipped.strength + enhanceStrengthGain} ({cost}g)";
        MenuOption option = new MenuOption(label, () => EnhanceWeapon(equipped, capturedOwner));
        option.enabled = !atMax;
        options.Add(option);
    }
    return options;
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
            owner.Equip(enhanced);
        }
        else
         {
        InventoryManager.Instance.LoseItem(original);
        InventoryManager.Instance.PickupEquipment(enhanced); 
        }
        if(forgeFeedbackText != null) forgeFeedbackText.text = $"Enhanced to {enhanced.equipmentName}.";
        host.RefreshScreen(EnhanceListMenu());
    }
    string StripSuffix(string name)
    {
        int plusIndex = name.LastIndexOf(" +");
        return plusIndex >= 0 ? name.Substring(0, plusIndex) : name;
    }
    List<MenuOption> ElementListMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach(Equipment equipment in InventoryManager.Instance.items.OfType<Equipment>())
        {
            if(equipment.equipmentType != Equipment.EquipmentType.Weapon) continue;
            if(equipment.element != Element.None) continue;
            Equipment captured = equipment;
            options.Add(new MenuOption
            ($"{captured.equipmentName} ({addElementCost}g)", () => OpenElementPicker(captured)));
        }
        return options;
    }
      void OpenElementPicker(Equipment weapon)
    {
        selectedWeaponForElement = weapon;
        host.OpenScreen(ElementPickerMenu(), weapon.equipmentName);
    }
    List<MenuOption> ElementPickerMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach(Element element in System.Enum.GetValues(typeof(Element)))
        {
            if(element == Element.None) continue;
            Element captured = element;
            options.Add(new MenuOption(captured.ToString(), () => AddElement(captured)));
        }
        return options;
    }
    void AddElement(Element element)
    {
        Equipment original = selectedWeaponForElement;
        if(!TrySpendForge(addElementCost)) return;
        Equipment enhanced = Instantiate(original);
        enhanced.baseAssetName = string.IsNullOrEmpty(original.baseAssetName) ? original.name : original.baseAssetName;
        enhanced.element = element;
        enhanced.equipmentName = $"{original.equipmentName} ({element})";
        InventoryManager.Instance.LoseItem(original);
        InventoryManager.Instance.PickupEquipment(enhanced);
        if(forgeFeedbackText != null) forgeFeedbackText.text = $"{enhanced.equipmentName} imbued with {element}";
        host.PopAndRefresh(ElementListMenu());
    }
     List<MenuOption> SmeltCraftMenu()
    {
        return new List<MenuOption>
        {
            new MenuOption("Smelt", SmeltListMenu),
            new MenuOption("Craft", CraftListMenu)
        };
    }
    List<MenuOption> SmeltListMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach(Equipment equipment in InventoryManager.Instance.items.OfType<Equipment>())
        {
            if(equipment.smeltYield == null || equipment.smeltYield.Count == 0) continue;
            Equipment captured = equipment;
            string yieldText = string.Join(", ", captured.smeltYield.Select(material => $"{material.amount}x {material.material.itemName}"));
        options.Add(new MenuOption($"{captured.equipmentName} -> {yieldText}", () => SmeltWeapons(captured)));
        }
        return options;
    }
    void SmeltWeapons(Equipment equipment)
    {
        InventoryManager.Instance.LoseItem(equipment);
        foreach (MaterialAmount material in equipment.smeltYield)
        {
            for(int i = 0; i < material.amount; i++) InventoryManager.Instance.PickupItem(material.material);
        }
        if(forgeFeedbackText != null) forgeFeedbackText.text = $"Smelted{equipment.equipmentName}.";
        host.RefreshScreen(SmeltListMenu());
    }
    List<MenuOption> CraftListMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach (CraftRecipe recipe in craftRecipes)
        {
            if(recipe == null) continue;
            if(recipe.result == null && recipe.itemResult == null) continue;
            CraftRecipe captured = recipe;
            string resultName = recipe.result != null ? recipe.result.equipmentName : recipe.itemResult.itemName;
            string costText = string.Join(", ", captured.requiredMaterials.Select(materials => $"{materials.amount}x {materials.material.itemName}"));
     MenuOption option = new MenuOption($"{resultName} ({costText})", () => CraftEquipment(captured));
     option.enabled = HasMaterials(captured);
     options.Add(option);
        }
        return options;
    }
    bool HasMaterials(CraftRecipe recipe)
    {
        foreach(MaterialAmount required in recipe.requiredMaterials)
        {
            int owned = InventoryManager.Instance.items.FindAll(item => item == required.material).Count;
            if (owned < required.amount) return false;
        }
        return true;
    }
    void CraftEquipment(CraftRecipe recipe)
    {
        if(!HasMaterials(recipe)){if(forgeFeedbackText != null) forgeFeedbackText.text = "Missing materials."; return;}
  foreach(MaterialAmount required in recipe.requiredMaterials)
        {
            for(int i = 0; i < required.amount; i++) InventoryManager.Instance.LoseItem(required.material);
        }
        string craftedName;
        if(recipe.result != null)
        {
        Equipment crafted = Instantiate(recipe.result);
        InventoryManager.Instance.PickupEquipment(crafted);
        craftedName = crafted.equipmentName;
        }
        else
        {
            InventoryManager.Instance.PickupItem(recipe.itemResult);
            craftedName = recipe.itemResult.itemName;
        }
        if(forgeFeedbackText != null) forgeFeedbackText.text = $"Crafted{craftedName}.";
        host.RefreshScreen(CraftListMenu());
    }
    List<MenuOption> AlchemyFirstItemMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach (Baggable item in InventoryManager.Instance.items)
        {
            Baggable captured = item;
            options.Add(new MenuOption(captured.DisplayName, () => OpenAlchemySecondItem(captured)));
        }
        return options;
    }
    void OpenAlchemySecondItem(Baggable first)
    {
        selectedFirstItem = first;
        host.OpenScreen(AlchemySecondItemMenu(), first.DisplayName);
    }
    List<MenuOption> AlchemySecondItemMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        List<Baggable> remaining = new List<Baggable>(InventoryManager.Instance.items);
        remaining.Remove(selectedFirstItem);
        foreach (Baggable item in remaining)
        {
            Baggable captured = item;
            options.Add(new MenuOption(captured.DisplayName, () => CombineItems(captured)));
        }
        return options;
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
            host.ClearScreenHistory();
           host.OpenScreen(ForgeMainMenu(), "Forge");
            return;
        }
        InventoryManager.Instance.LoseItem(first);
        InventoryManager.Instance.LoseItem(second);
        if(matched.result is Item resultItem) InventoryManager.Instance.PickupItem(resultItem);
        else if(matched.result is Equipment resultEquipment) InventoryManager.Instance.PickupEquipment(Instantiate(resultEquipment));
        if(forgeFeedbackText != null) forgeFeedbackText.text = $"{first.DisplayName} + {second.DisplayName} became {matched.result.DisplayName}.";
    host.ClearScreenHistory();
    host.OpenScreen(ForgeMainMenu(), "Forge");
    }
}
