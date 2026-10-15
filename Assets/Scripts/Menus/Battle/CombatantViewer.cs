using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
public class CombatantViewer : MonoBehaviour
{
    public static CombatantViewer instance;
    public GameObject display;
    public TextMeshProUGUI headerText;
    public Image hpFill;
    public Image mpFill;
    public TextMeshProUGUI coreStatsText;
    public TextMeshProUGUI secondaryStatsText;
    public TextMeshProUGUI equipmentText;
    public TextMeshProUGUI personalInventoryText;
    public TextMeshProUGUI equippedSkillsText;
    public TextMeshProUGUI extraText;
    public Image icon;
    const string colorMuted = "#8A8580";
    const string colorBright = "#E8E4E0";
    const string colorDone = "#639922";
    const string dot = "\u00B7";
    ActiveStats shownCharacter;
    EnemyAI shownEnemy;
    void Awake()
    {
        if(instance == null) instance = this; else Destroy(gameObject);
    }
    void Start()
    {
        if(display != null) display.SetActive(false);
    }
    public bool IsOpen => display != null && display.activeSelf;
    public void Open(ActiveStats character)
    {
        shownCharacter = character;
        shownEnemy = null;
        if(display != null) display.SetActive(true);
        Refresh();
    }
    public void Open(EnemyAI enemy)
    {
        shownEnemy = enemy;
        shownCharacter = null;
        if(display != null) display.SetActive(true);
        Refresh();
    }
    public void Close()
    {
        if(display != null) display.SetActive(false);
        shownCharacter = null;
        shownEnemy = null;
    }
    void Refresh()
    {
        if(icon != null) icon.sprite = null;
        if(shownCharacter != null) RefreshPlayer(shownCharacter);
        else if(shownEnemy != null) RefreshEnemy(shownEnemy);
    }
    void RefreshPlayer(ActiveStats character)
    {
        if(headerText != null)
        headerText.text = $"{character.currentName} Lv.{character.currentLevel}\nHP {character.currentHP}/{character.finalHP}\nMP {character.currentMP}/{character.finalMP}\n" +
        $"Exp {character.currentExperience}/{character.currentExpToNextLevel} (Total {character.totalExperience})";
        SetBar(hpFill, character.currentHP, character.finalHP);
        SetBar(mpFill, character.currentMP, character.finalMP);
        SetCoreStats(character.finalStrength, character.finalMagic, character.finalDefense, character.finalWisdom,
        character.finalTech, character.finalAffinity, character.finalSpeed, character.finalLuck, character.currentMagicAffinity);
        SetSecondaryStats(character.Accuracy, character.Precision, character.Evasion, character.Foresight, character.Critical, character.Dodge);
        SetEquipment(character.weaponSlot, character.headSlot, character.bodySlot, character.shieldSlot, character.accessorySlot);
        SetPersonalInventory(character.personalInventory.items);
        SetEquippedSkills(character.equippedSkills);
        if(extraText != null)
        {
            List<string> lines = new List<string>();
            foreach(BondProgress progress in character.bondProgress)
            {
                if(progress.partner == null) continue;
                lines.Add($"{progress.partner.characterName}: {character.GetBondRank(progress.partner)}");
            }
            string body = lines.Count > 0 ? string.Join("\n", lines) : "No bonds formed";
            extraText.text = $"<size=75%><color={colorMuted}>BONDS</color></size>\n{body}";
        }
    }
    void RefreshEnemy(EnemyAI enemy)
    {
        EnemyStats stats = enemy.enemyStats;
        if(headerText != null)
        headerText.text = $"{enemy.currentName} Lv.{enemy.currentLevel}\nHP {enemy.currentHP}/{enemy.finalHP}\nMP {enemy.currentMP}/{enemy.finalMP}\n";
        SetBar(hpFill, enemy.currentHP, enemy.finalHP);
        SetBar(mpFill, enemy.currentMP, enemy.finalMP);
        SetCoreStats(enemy.finalStrength, enemy.finalMagic, enemy.finalDefense, enemy.finalWisdom,
        enemy.finalTech, enemy.finalAffinity, enemy.finalSpeed, enemy.finalLuck, enemy.currentMagicAffinity);
        SetSecondaryStats(enemy.Accuracy, enemy.Precision, enemy.Evasion, enemy.Foresight, enemy.Critical, enemy.Dodge);
        SetEquipment(enemy.weaponSlot, enemy.headSlot, enemy.bodySlot, enemy.shieldSlot, enemy.accessorySlot);
        SetPersonalInventory(enemy.personalInventory.items);
        SetEquippedSkills(enemy.equippedSkills);
        if(extraText != null) extraText.text = stats != null ? BuildEnemyDetail(stats) : "";
    }
    void SetBar(Image fill, int current, int max)
    {
        if(fill != null) fill.fillAmount = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
    }
    void SetCoreStats(int strength, int magic, int defense, int wisdom, int tech, int affinity, int speed, int luck, Element magicAffinity)
    {
        if(coreStatsText == null) return;
        coreStatsText.text = 
        $"<size=75%><color={colorMuted}>STATS</color></size>\n" +
        $"Strength: {strength}       Magic: {magic}\n" +
        $"Defense: {defense}         Wisdom: {wisdom}\n" +
        $"Tech: {tech}               Affinity: {affinity}\n" +
        $"Speed: {speed}             Luck: {luck}\n" + 
        $"Magic Affinity: {magicAffinity}";
    }
    void SetSecondaryStats(int accuracy, int precision, int evasion, int foresight, int critical, int dodge)
    {
        if(secondaryStatsText == null) return;
        secondaryStatsText.text =
        $"<size=75%><color={colorMuted}>SECONDARY STATS</color></size>\n" + 
        $"Accuracy: {accuracy}             Precision: {precision}\n" +
        $"Evasion: {evasion}                 Foresight: {foresight}\n" + 
        $"Critical: {critical}               Dodge: {dodge}";
    }
    void SetEquipment(Equipment weapon, Equipment head, Equipment body, Equipment shield, Equipment accessory)
    {
        if (equipmentText == null) return;
        string Slot(Equipment equipment, string label) => $"{label}: {(equipment != null ? equipment.equipmentName : "Empty")}";
        equipmentText.text = $"<size=75%><color={colorMuted}>EQUIPMENT</color></size>\n" + string.Join("\n", new[]
            {
                Slot(weapon, "Weapon"), Slot(head, "Head"), Slot(body, "Body"), Slot(shield, "Shield"), Slot(accessory, "Accessory")
            });
    }
    void SetPersonalInventory(List<Baggable> items)
    {
          if(personalInventoryText == null) return;
          string header = $"<size=75%><color={colorMuted}>INVENTORY</color></size>\n";
          if(items == null || items.Count == 0) { personalInventoryText.text = header + "Empty"; return; }
          Dictionary<Baggable, int> counts = new Dictionary<Baggable, int>();
          List<Baggable> order = new List<Baggable>();
          foreach(Baggable item in items)
        {
            if(item == null) continue;
            if(!counts.ContainsKey(item)) { counts[item] = 0; order.Add(item); }
            counts[item]++;
        }
        personalInventoryText.text = header + (order.Count > 0
        ? string.Join("\n", order.Select(item => $"{item.DisplayName} x{counts[item]}"))
        : "Empty");
    }
    void SetEquippedSkills(List<Skill> skills)
    {
        if(equippedSkillsText == null) return;
        List<string> names = skills.Where(skill => skill != null).Select(skill => skill.skillName).ToList();
        string body = names.Count > 0 ? string.Join("\n", names) : "None";
        equippedSkillsText.text = $"<size=75%><color={colorMuted}>SKILLS</color></size>\n{body}";
    }
    string BuildEnemyDetail(EnemyStats stats)
    {
        string result = $"<size=75%><color={colorMuted}>RACE</color></size>\n<color={colorBright}>{stats.race}</color>\n\n";
        if(!string.IsNullOrEmpty(stats.dexEntry))
        result += $"<color={colorBright}>{stats.dexEntry}</color>\n\n";
        if(stats.immunities != null && stats.immunities.Count > 0)
        {
            List<string> immuneNames = new List<string>();
            foreach(StatusEffect status in stats.immunities) if(status != null) immuneNames.Add(status.name);
            result += $"<size=75%><color={colorMuted}>IMMUNE TO</color></size>\n<color={colorBright}>{string.Join(", ", immuneNames)}</color>\n\n";
        }
        if(stats.lootTable != null && stats.lootTable.Count > 0)
        {
            result += $"<size=75%><color={colorMuted}>DROPS</color></size>\n";
            foreach(EnemyStats.LootDrop drop in stats.lootTable)
            {
                if(drop == null || drop.item == null) continue;
                result += $"<color={colorBright}>{drop.item.itemName}</color> <color={colorDone}>{drop.dropChance}%</color>\n";
            }
            result += "\n";
        }
        if(stats.stealTable != null && stats.stealTable.Count > 0)
        {
            result += $"<size=75%><color={colorMuted}>STEALABLE</color></size>\n";
            foreach(EnemyStats.StealDrop steal in stats.stealTable)
            {
                if(steal == null || steal.item == null) continue;
                result += $"<color={colorBright}>{steal.item.itemName}</color> <color={colorDone}>{steal.stealChance}%</color>\n";
            }
        }
        return result;
    }
}
    