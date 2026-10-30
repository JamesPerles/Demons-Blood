using UnityEngine;
using System.Collections.Generic;
public class StatCalculatorResult
{
public int finalHP, finalMP, finalStrength, finalMagic, finalDefense, finalWisdom, finalTech, finalAffinity, finalSpeed, finalLuck;
public int accuracy, precision, evasion, foresight, critical, dodge;
}
public class StatCalculator
{
    Dictionary<Stat, int> statStages = new Dictionary<Stat, int>();
    public int GetStatStage(Stat stat) => statStages.TryGetValue(stat, out int value) ? value : 0;
    public void ChangeStatStage(Stat stat, int amount)
    {
        int current = GetStatStage(stat);
        statStages[stat] = Mathf.Clamp(current + amount, StatStage.MinStage, StatStage.MaxStage);
    }
    public void ResetStatStages() => statStages.Clear();
    void ApplyEquipmentSlot(Equipment equipment, StatCalculatorResult result)
    {
        if(equipment == null) return;
        result.finalHP += equipment.hp;
        result.finalMP += equipment.mp;
        result.finalStrength += equipment.strength;
        result.finalMagic += equipment.magic;
        result.finalDefense += equipment.defense;
        result.finalWisdom += equipment.wisdom;
        result.finalTech += equipment.tech;
        result.finalAffinity += equipment.affinity;
        result.finalSpeed += equipment.speed;
        result.finalLuck += equipment.luck;
        result.accuracy += equipment.Accuracy;
        result.evasion += equipment.Evasion;
        result.precision += equipment.Precision;
        result.foresight += equipment.Foresight;
        result.critical += equipment.Critical;
        result.dodge += equipment.Dodge;
    }
    public StatCalculatorResult Recalculate(int maxHP, int maxMP, int strength, int magic, int defense, 
    int wisdom, int tech, int affinity, int speed, int luck, IEnumerable<Equipment> equipmentSlots,
    float transformMultiplier, System.Action<StatCalculatorResult> applyFlatBonuses = null)
    {
        StatCalculatorResult result = new StatCalculatorResult
        {
            finalHP = maxHP, finalMP = maxMP, finalStrength = strength, finalMagic = magic,
            finalDefense = defense, finalWisdom = wisdom, finalTech = tech, finalAffinity = affinity,
            finalSpeed = speed, finalLuck = luck
        };
        if(equipmentSlots != null)
        foreach(Equipment equipment in equipmentSlots) ApplyEquipmentSlot(equipment, result);
        applyFlatBonuses?.Invoke(result);
        result.finalHP = Mathf.RoundToInt(result.finalHP * StatStage.Multiplier(GetStatStage(Stat.HP)));
        result.finalMP = Mathf.RoundToInt(result.finalMP * StatStage.Multiplier(GetStatStage(Stat.MP)));
        result.finalStrength = Mathf.RoundToInt(result.finalStrength * StatStage.Multiplier(GetStatStage(Stat.Strength)));
        result.finalMagic = Mathf.RoundToInt(result.finalMagic * StatStage.Multiplier(GetStatStage(Stat.Magic)));
        result.finalDefense = Mathf.RoundToInt(result.finalDefense * StatStage.Multiplier(GetStatStage(Stat.Defense)));
        result.finalWisdom = Mathf.RoundToInt(result.finalWisdom * StatStage.Multiplier(GetStatStage(Stat.Wisdom)));
        result.finalTech = Mathf.RoundToInt(result.finalTech * StatStage.Multiplier(GetStatStage(Stat.Tech)));
        result.finalAffinity = Mathf.RoundToInt(result.finalAffinity * StatStage.Multiplier(GetStatStage(Stat.Affinity)));
        result.finalSpeed = Mathf.RoundToInt(result.finalSpeed * StatStage.Multiplier(GetStatStage(Stat.Speed)));
        result.finalLuck = Mathf.RoundToInt(result.finalLuck * StatStage.Multiplier(GetStatStage(Stat.Luck)));
        result.finalHP = Mathf.RoundToInt(result.finalHP * transformMultiplier);
        result.finalMP = Mathf.RoundToInt(result.finalMP * transformMultiplier);
        result.finalStrength = Mathf.RoundToInt(result.finalStrength * transformMultiplier);
        result.finalMagic = Mathf.RoundToInt(result.finalMagic * transformMultiplier);
        result.finalDefense = Mathf.RoundToInt(result.finalDefense * transformMultiplier);
        result.finalWisdom = Mathf.RoundToInt(result.finalWisdom * transformMultiplier);
        result.finalTech = Mathf.RoundToInt(result.finalTech * transformMultiplier);
        result.finalAffinity = Mathf.RoundToInt(result.finalAffinity * transformMultiplier);
        result.finalSpeed = Mathf.RoundToInt(result.finalSpeed * transformMultiplier);
        result.finalLuck = Mathf.RoundToInt(result.finalLuck * transformMultiplier);
        result.accuracy  = Mathf.RoundToInt(result.accuracy * StatStage.Multiplier(GetStatStage(Stat.Accuracy)));
        result.precision = Mathf.RoundToInt(result.precision * StatStage.Multiplier(GetStatStage(Stat.Precision)));
        result.evasion = Mathf.RoundToInt(result.evasion * StatStage.Multiplier(GetStatStage(Stat.Evasion)));
        result.foresight = Mathf.RoundToInt(result.foresight * StatStage.Multiplier(GetStatStage(Stat.Foresight)));
        result.critical = Mathf.RoundToInt(result.critical * StatStage.Multiplier(GetStatStage(Stat.Critical)));
        result.dodge = Mathf.RoundToInt(result.dodge * StatStage.Multiplier(GetStatStage(Stat.Dodge)));
        return result;
    }
}
