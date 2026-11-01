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
    Dictionary<object, float> momentumBySource = new Dictionary<object, float>();
    public void SetMomentum(object source, float percent)
    {
        if(Mathf.Approximately(percent, 0f)) momentumBySource.Remove(source);
        else momentumBySource[source] = percent;
    }
    public void ResetStatStages() { statStages.Clear(); momentumBySource.Clear(); }
    public bool HasDebuff()
    {
        foreach(KeyValuePair<Stat, int> pair in statStages) if(pair.Value < 0) return true;
        float total = 0f;
        foreach(float value in momentumBySource.Values) total += value;
        return total < 0f;
    }
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
    float transformMultiplier, System.Action<StatCalculatorResult> applyFlatBonuses = null, System.Func<Stat, float> passiveMultiplier = null)
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
        result.accuracy += result.finalTech + (result.finalLuck / 3) + (result.finalSpeed / 2);
        result.precision += result.finalAffinity + (result.finalLuck / 3) + (result.finalSpeed / 2);
        result.evasion += (result.finalLuck / 2) + (result.finalSpeed / 2) + (result.finalTech / 2) + (result.finalDefense / 5) + (result.finalStrength / 5);
        result.foresight += (result.finalLuck / 2) + (result.finalSpeed / 2) + (result.finalWisdom / 2) + (result.finalAffinity / 3) + (result.finalMagic / 4);
        result.critical += (result.finalTech / 3) + (result.finalAffinity / 3) + (result.finalLuck / 2);
        result.dodge += (result.evasion / 2) + (result.finalLuck / 2);
        result.accuracy  = Mathf.RoundToInt(result.accuracy * StatStage.Multiplier(GetStatStage(Stat.Accuracy)));
        result.precision = Mathf.RoundToInt(result.precision * StatStage.Multiplier(GetStatStage(Stat.Precision)));
        result.evasion = Mathf.RoundToInt(result.evasion * StatStage.Multiplier(GetStatStage(Stat.Evasion)));
        result.foresight = Mathf.RoundToInt(result.foresight * StatStage.Multiplier(GetStatStage(Stat.Foresight)));
        result.critical = Mathf.RoundToInt(result.critical * StatStage.Multiplier(GetStatStage(Stat.Critical)));
        result.dodge = Mathf.RoundToInt(result.dodge * StatStage.Multiplier(GetStatStage(Stat.Dodge)));
        float momentum = 0f;
        foreach(float value in momentumBySource.Values) momentum += value;
        float allMultiplier = Mathf.Max(0f, 1f + momentum);
        result.finalStrength = Mathf.RoundToInt(result.finalStrength * allMultiplier);
        result.finalMagic = Mathf.RoundToInt(result.finalMagic * allMultiplier);
        result.finalDefense = Mathf.RoundToInt(result.finalDefense * allMultiplier);
        result.finalWisdom = Mathf.RoundToInt(result.finalWisdom * allMultiplier);
        result.finalTech = Mathf.RoundToInt(result.finalTech * allMultiplier);
        result.finalAffinity = Mathf.RoundToInt(result.finalAffinity * allMultiplier);
        result.finalSpeed = Mathf.RoundToInt(result.finalSpeed * allMultiplier);
        result.finalLuck = Mathf.RoundToInt(result.finalLuck * allMultiplier);
        result.accuracy  = Mathf.RoundToInt(result.accuracy * allMultiplier);
        result.precision = Mathf.RoundToInt(result.precision * allMultiplier);
        result.evasion = Mathf.RoundToInt(result.evasion * allMultiplier);
        result.foresight = Mathf.RoundToInt(result.foresight * allMultiplier);
        result.critical = Mathf.RoundToInt(result.critical * allMultiplier);
        result.dodge = Mathf.RoundToInt(result.dodge * allMultiplier);
        if(passiveMultiplier != null)
        {
        result.finalHP = Mathf.RoundToInt(result.finalHP * passiveMultiplier(Stat.HP));
        result.finalMP = Mathf.RoundToInt(result.finalMP * passiveMultiplier(Stat.MP));
        result.finalStrength = Mathf.RoundToInt(result.finalStrength * passiveMultiplier(Stat.Strength));
        result.finalMagic = Mathf.RoundToInt(result.finalMagic * passiveMultiplier(Stat.Magic));
        result.finalDefense = Mathf.RoundToInt(result.finalDefense * passiveMultiplier(Stat.Defense));
        result.finalWisdom = Mathf.RoundToInt(result.finalWisdom * passiveMultiplier(Stat.Wisdom));
        result.finalTech = Mathf.RoundToInt(result.finalTech * passiveMultiplier(Stat.Tech));
        result.finalAffinity = Mathf.RoundToInt(result.finalAffinity * passiveMultiplier(Stat.Affinity));
        result.finalSpeed = Mathf.RoundToInt(result.finalSpeed * passiveMultiplier(Stat.Speed));
        result.finalLuck = Mathf.RoundToInt(result.finalLuck * passiveMultiplier(Stat.Luck));
        result.accuracy  = Mathf.RoundToInt(result.accuracy * passiveMultiplier(Stat.Accuracy));
        result.precision = Mathf.RoundToInt(result.precision * passiveMultiplier(Stat.Precision));
        result.evasion = Mathf.RoundToInt(result.evasion * passiveMultiplier(Stat.Evasion));
        result.foresight = Mathf.RoundToInt(result.foresight * passiveMultiplier(Stat.Foresight));
        result.critical = Mathf.RoundToInt(result.critical * passiveMultiplier(Stat.Critical));
        result.dodge = Mathf.RoundToInt(result.dodge * passiveMultiplier(Stat.Dodge));
        }
        return result;
    }
}
