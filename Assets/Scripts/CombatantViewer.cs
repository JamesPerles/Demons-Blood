using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
public class CombatantViewer : MonoBehaviour
{
    public GameObject display;
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI coreStatsText;
    public TextMeshProUGUI secondaryStatsText;
    public TextMeshProUGUI equipmentText;
    public TextMeshProUGUI bondText;
    public TextMeshProUGUI equippedSkillsText;
    ActiveStats shownCharacter;
    public void Open(ActiveStats character)
    {
        shownCharacter = character;
        if(display != null) display.SetActive(true);
        Refresh();
    }
    public void Close()
    {
        if(display != null) display.SetActive(false);
    }
    void Refresh()
    {
        if(shownCharacter == null) return;
        ActiveStats character = shownCharacter;
        if(headerText != null)
        headerText.text = $"{character.currentName} Lv.{character.currentLevel}\nHP {character.currentHP}/{character.finalHP} MP {character.currentMP}/{character.finalMP}\n" +
        $"Exp {character.currentExperience}/{character.currentExpToNextLevel} (Total {character.totalExperience})";
        if(coreStatsText != null)
        coreStatsText.text = 
        $"Strength: {character.finalStrength}\nMagic: {character.finalMagic}\nDefense: {character.finalDefense}\nWisdom: {character.finalWisdom}\n" +
        $"Tech: {character.finalTech}\nAffinity: {character.finalAffinity}\nSpeed: {character.finalSpeed}\nLuck: {character.finalLuck}\n" + 
        $"Magic Affinity: {character.currentMagicAffinity}";
        if(secondaryStatsText != null)
        secondaryStatsText.text = 
        $"Accuracy: {character.Accuracy}\nPrecision: {character.Precision}\nEvasion: {character.Evasion}\n" +
        $"Foresight: {character.Foresight}\nCritical: {character.Critical}\nDodge: {character.Dodge}";
        if (equipmentText != null)
        {
            string Slot(Equipment equipment, string label) => $"{label}: {(equipment != null ? equipment.equipmentName : "Empty")}";
            equipmentText.text = string.Join("\n", new[]
            {
                Slot(character.weaponSlot, "Weapon"), Slot(character.headSlot, "Head"), Slot(character.bodySlot, "Body"),
                Slot(character.shieldSlot, "Shield"), Slot(character.accessorySlot, "Accessory")
            });
        }
        if (bondText != null)
        {
            List<string> lines = new List<string>();
            foreach(BondProgress progress in character.bondProgress)
            {
                if(progress.partner == null) continue;
                lines.Add($"{progress.partner.characterName}: {character.GetBondRank(progress.partner)}");
            }
            bondText.text = lines.Count > 0 ? string.Join("\n", lines) : "No bonds formed";
        }
        if(equippedSkillsText != null)
        {
            List<string> names = character.equippedSkills.Where(skill => skill != null).Select(skill => skill.skillName).ToList();
            equippedSkillsText.text = names.Count > 0 ? string.Join("\n", names) : "None";
        }
    }
}