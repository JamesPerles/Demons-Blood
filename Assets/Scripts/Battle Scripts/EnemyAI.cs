using UnityEngine;
using System.Collections.Generic;
public class EnemyAI : MonoBehaviour , ICombatant
{
   public int currentHP {get; set;}
    public int currentMP {get; set;}
    int currentStrength;
    int currentMagic;
    int currentDefense;
    int currentWisdom;
    int currentTech;
    int currentAffinity;
    int currentSpeed;
    int currentLuck;
    public int maxHP;
    public int maxMP;
    public int finalHP {get; set;}
    public int finalMP {get; set;}
    public int finalStrength {get; set;}
    public int finalMagic {get; set;}
    public int finalDefense {get; set;}
    public int finalWisdom {get; set;}
    public int finalTech {get; set;}
    public int finalAffinity {get; set;}
    public int finalSpeed {get; set;}
    public int finalLuck {get; set;}
    public int currentLevel;
    public string currentName {get; set;}
    public int currentAttackChance;
    public int currentDefendChance;
    public int currentItemChance;
    public int currentRunChance;
    public int currentHPThreshold;
    public int currentIncreaseAttackChance;
    public int currentIncreaseDefendChance;
    public int currentIncreaseItemChance;
    public int currentIncreaseRunChance;
    public EnemyStats enemyStats;
    public bool isDefending {get; set;}
    public Element currentMagicAffinity {get; set;}
    public List<StatusEffect> statusImmunities = new List<StatusEffect>();
    public int Accuracy {get; set;}
    public int Precision {get; set;}
    public int Evasion {get; set;}
    public int Foresight {get; set;}
    public int Critical {get; set;}
    public int Dodge {get; set;}
    public Equipment weaponSlot;
    public Equipment headSlot;
    public Equipment bodySlot;
    public Equipment shieldSlot;
    public Equipment accessorySlot;
    public int baseTargetWeight = 25;
    public int lowHPWeightBonus = 30;
    public int killWeightBonus = 50;
    public int defendingWeightBonus = 20;
    public int targetHPThreshold = 30;
    public int minGoldReward = 10;
    public int maxGoldReward = 35;
    public List<Spell> learnedSpells = new List<Spell>();
    public List<Art> learnedArts = new List<Art>();
    public List<Fusion> learnedFusions = new List<Fusion>();
    public List<Skill> learnedSkills {get; set;} = new List<Skill>();
   public List<Skill> equippedSkills => learnedSkills;
   public List<ActiveStatusEffect> activeStatuses {get; set;} = new List<ActiveStatusEffect>();
    public float transformMultiplier = 1f;
    public bool isTransformed {get; protected set;} = false;
    public float transformHPThreshold = 0f;
    public float bossTransformMultiplier = 1.3f;
    Dictionary<Stat, int> statStages = new Dictionary<Stat, int>();
    public int GetStatStage(Stat stat) => statStages.TryGetValue(stat, out int value) ? value : 0;
    public void ChangeStatStage(Stat stat, int amount)
    {
        int current = GetStatStage(stat);
        statStages[stat] = Mathf.Clamp(current + amount, StatStageUtility.MinStage, StatStageUtility.MaxStage);
        RecalculateStats();
    }
    void Awake()
    {
        currentName = enemyStats.enemyName;
        maxHP = enemyStats.hp;
        maxMP = enemyStats.mp;
        currentHP = maxHP;
        currentMP = maxMP;
        currentStrength = enemyStats.strength;
        currentMagic = enemyStats.magic;
        currentDefense = enemyStats.defense;
        currentWisdom = enemyStats.wisdom;
        currentTech = enemyStats.tech;
        currentAffinity = enemyStats.affinity;
        currentSpeed = enemyStats.speed;
        currentLuck = enemyStats.luck;
        currentLevel = enemyStats.level;
        currentAttackChance = enemyStats.attackChance;
        currentDefendChance = enemyStats.defendChance;
        currentItemChance = enemyStats.itemChance;
        currentRunChance = enemyStats.runChance;
        currentHPThreshold = enemyStats.hpthreshold;
        currentIncreaseAttackChance = enemyStats.increaseAttackChance;
        currentIncreaseDefendChance = enemyStats.increaseDefendChance;
        currentIncreaseItemChance = enemyStats.increaseItemChance;
        currentIncreaseRunChance = enemyStats.increaseRunChance;
        currentMagicAffinity = enemyStats.magicAffinity;
        statusImmunities  = new List<StatusEffect>(enemyStats.immunities);
        RecalculateStats();
    }
    public bool IsImmuneToStatus(StatusEffect status) => status != null && statusImmunities.Contains(status);
    void OnValidate()
    {
        if (weaponSlot != null && weaponSlot.equipmentType != Equipment.EquipmentType.Weapon) weaponSlot = null;
        if (headSlot != null && headSlot.equipmentType != Equipment.EquipmentType.Head) headSlot = null;
        if (bodySlot != null && bodySlot.equipmentType != Equipment.EquipmentType.Body) bodySlot = null;
        if (shieldSlot != null && shieldSlot.equipmentType != Equipment.EquipmentType.Shield) shieldSlot = null;
        if (accessorySlot != null && accessorySlot.equipmentType != Equipment.EquipmentType.Accessory) accessorySlot = null;
        RecalculateStats();
    } 
    void ApplyEquipmentSlot(Equipment equipment)
    {
        if(equipment == null) return;
        finalHP += equipment.hp;
        finalMP += equipment.mp;
        finalStrength += equipment.strength;
        finalMagic += equipment.magic;
        finalDefense += equipment.defense;
        finalWisdom += equipment.wisdom;
        finalTech += equipment.tech;
        finalAffinity += equipment.affinity;
        finalSpeed += equipment.speed;
        finalLuck += equipment.luck;
        Accuracy += equipment.Accuracy;
        Evasion += equipment.Evasion;
        Precision += equipment.Precision;
        Foresight += equipment.Foresight;
        Critical += equipment.Critical;
        Dodge += equipment.Dodge;
    }
    void RecalculateStats()
    {
        finalHP = maxHP;
        finalMP = maxMP;
        finalStrength = currentStrength;
        finalMagic = currentMagic;
        finalDefense = currentDefense;
        finalWisdom = currentWisdom;
        finalTech = currentTech;
        finalAffinity = currentAffinity;
        finalSpeed = currentSpeed;
        finalLuck = currentLuck;
        ApplyEquipmentSlot(weaponSlot);
        ApplyEquipmentSlot(headSlot);
        ApplyEquipmentSlot(bodySlot);
        ApplyEquipmentSlot(shieldSlot);
        ApplyEquipmentSlot(accessorySlot);
        finalHP = Mathf.RoundToInt(finalHP * StatStageUtility.Multiplier(GetStatStage(Stat.HP)));
       finalMP = Mathf.RoundToInt(finalMP * StatStageUtility.Multiplier(GetStatStage(Stat.MP)));
       finalStrength = Mathf.RoundToInt(finalStrength * StatStageUtility.Multiplier(GetStatStage(Stat.Strength)));
       finalMagic = Mathf.RoundToInt(finalMagic * StatStageUtility.Multiplier(GetStatStage(Stat.Magic)));
       finalDefense = Mathf.RoundToInt(finalDefense * StatStageUtility.Multiplier(GetStatStage(Stat.Defense)));
       finalWisdom = Mathf.RoundToInt(finalWisdom * StatStageUtility.Multiplier(GetStatStage(Stat.Wisdom)));
       finalTech = Mathf.RoundToInt(finalTech * StatStageUtility.Multiplier(GetStatStage(Stat.Tech)));
       finalAffinity = Mathf.RoundToInt(finalAffinity * StatStageUtility.Multiplier(GetStatStage(Stat.Affinity)));
       finalSpeed = Mathf.RoundToInt(finalSpeed * StatStageUtility.Multiplier(GetStatStage(Stat.Speed)));
       finalLuck = Mathf.RoundToInt(finalLuck * StatStageUtility.Multiplier(GetStatStage(Stat.Luck)));
        finalHP = Mathf.RoundToInt(finalHP * transformMultiplier);
        finalMP = Mathf.RoundToInt(finalMP * transformMultiplier);
        finalStrength = Mathf.RoundToInt(finalStrength * transformMultiplier);
        finalMagic = Mathf.RoundToInt(finalMagic * transformMultiplier);
        finalDefense = Mathf.RoundToInt(finalDefense * transformMultiplier);
        finalWisdom = Mathf.RoundToInt(finalWisdom * transformMultiplier);
        finalTech = Mathf.RoundToInt(finalTech * transformMultiplier);
        finalAffinity = Mathf.RoundToInt(finalAffinity * transformMultiplier);
        finalSpeed = Mathf.RoundToInt(finalSpeed * transformMultiplier);
        finalLuck = Mathf.RoundToInt(finalLuck * transformMultiplier);
        Accuracy = finalTech + (finalLuck / 3) + (finalSpeed / 2);
        Precision = finalAffinity + (finalLuck / 3) + (finalSpeed / 2);
        Evasion = (finalLuck / 2) + (finalSpeed / 2) + (finalTech / 2) + (finalDefense / 5) + (finalStrength / 5);
        Foresight = (finalLuck / 2) + (finalSpeed / 2) + (finalWisdom / 2) + (finalAffinity / 3) + (finalMagic / 4);
        Critical = (finalTech / 3) + (finalAffinity / 3) + (finalLuck / 2);
        Dodge = (Evasion / 2) + (finalLuck / 2);
        if(currentHP > finalHP) currentHP = finalHP;
        if(currentMP > finalMP) currentMP = finalMP;
    }
    public enum EnemyActionType {Attack, Defend, Item, Run}
    public enum EnemySpecialAttackCategory { Art, Spell, Fusion}
    [System.Serializable]
    public class EnemySpecialAttack
    {
        public string attackName;
        public EnemySpecialAttackCategory category;
        public Learnable learnable;
        public int weight = 10;
    }
    public List<EnemySpecialAttack> specialAttacks = new List<EnemySpecialAttack>();
    public int basicAttackWeight = 50;
    public int expReward = 10;
    [System.Serializable]
    public class LootDrop
    {
        public Item item;
        public int quantity = 1;
        [Range(0, 100)] public int dropChance = 100;
    }
    public List<LootDrop> lootTable = new List<LootDrop>();
    public virtual EnemySpecialAttack ChooseAttackMove()
    {
        List<EnemySpecialAttack> usable = specialAttacks.FindAll(learnable => CanAfford(learnable.learnable));
        int totalWeight = basicAttackWeight;
        foreach (var move in usable) totalWeight += move.weight;
        if (totalWeight <= 0) return null;
        int roll = Random.Range(0, totalWeight);
        if (roll < basicAttackWeight) return null;
        roll -= basicAttackWeight;
        foreach (var move in usable)
        {
            if (roll < move.weight) return move;
            roll -= move.weight;
        } return null;
    }
    bool CanAfford(Learnable learnable)
    {
        if (learnable is Art art) return currentHP >= art.Cost;
        if(learnable is Spell spell) return currentMP >= spell.Cost;
        if(learnable is Fusion fusion) return currentHP >= fusion.HPCost && currentMP >= fusion.MPCost;
        return false;
    }
    public void PaySpecialCost(Learnable learnable)
    {
        if(learnable is Art art) currentHP = Mathf.Max(0, currentHP - art.Cost);
        else if(learnable is Spell spell) currentMP = Mathf.Max(0, currentMP - spell.Cost);
        else if (learnable is Fusion fusion)
        {
            currentHP = Mathf.Max(0, currentHP - fusion.HPCost);
            currentMP = Mathf.Max(0, currentMP - fusion.MPCost);
        }
    }
    public virtual EnemyActionType ChooseAction()
    {
        int totalWeight = currentAttackChance + currentDefendChance + currentItemChance + currentRunChance;
        int roll = Random.Range(0, totalWeight);
        if (roll < currentAttackChance) return EnemyActionType.Attack;
        roll -= currentAttackChance;
        if(roll < currentDefendChance) return EnemyActionType.Defend;
        roll -= currentDefendChance;
        if(roll < currentItemChance) return EnemyActionType.Item;
        roll -= currentItemChance;
        return EnemyActionType.Run;
    } 
    public void Defend()
    {
        isDefending = true;
    }
    public void TakeDamage(int damage)
    {
        if (isDefending) damage = damage * 20/100;
        currentHP -= Mathf.Max(1, damage);
        currentHP = Mathf.Max(0, currentHP);
    }
    public void Heal(int amount)
    {
        currentHP = Mathf.Min(currentHP + amount, finalHP);
    }
    public void RestoreMP(int amount)
    {
        currentMP = Mathf.Min(currentMP + amount, finalMP);
    }
    public bool aiShifted = false;
    public virtual void ChangeAI()
    {
        if(currentHP < currentHPThreshold && !aiShifted)
        {
            currentAttackChance += currentIncreaseAttackChance;
            currentDefendChance += currentIncreaseDefendChance;
            currentItemChance += currentIncreaseItemChance;
            currentRunChance += currentIncreaseRunChance;
            aiShifted = true;
        }
    }
    public virtual void CheckTransform()
    {
        if(!isTransformed && transformHPThreshold > 0 && currentHP <= transformHPThreshold && currentHP > 0)
        {
            isTransformed = true;
            transformMultiplier = bossTransformMultiplier;
            RecalculateStats();
        }
    }
   public ActiveStats  ChooseTarget(List<ActiveStats> players)
    {
        Dictionary<ActiveStats, int> weights = new Dictionary<ActiveStats, int>();
        foreach (ActiveStats player in players)
        {
            int weight = baseTargetWeight;
            bool isLowHP = player.currentHP <= (player.finalHP * targetHPThreshold / 100);
            if(isLowHP) weight += lowHPWeightBonus; //what determines islowHP?
            int potentialDamage = Mathf.Max(1, finalStrength - player.finalDefense);
            bool wouldKill = player.currentHP <= potentialDamage;
            if(wouldKill) weight += killWeightBonus;
            if(player.isDefending) weight -= defendingWeightBonus;
            weights[player] = Mathf.Max(0, weight);
        }
        int totalWeight = 0;
        foreach (int weight in weights.Values) totalWeight += weight;
        if (totalWeight <= 0) return players[Random.Range(0, players.Count)];
        int roll = Random.Range(0, totalWeight);
        foreach (ActiveStats player in players)
        {
            if (roll < weights[player]) return player;
            roll -= weights[player];
        }
        return players[players.Count - 1]; 
    }
    public void ApplyStatus(StatusEffect status)
    {
        if(status == null) return;
        ActiveStatusEffect existing = activeStatuses.Find(active => active.statusEffect == status);
       if(existing != null) existing.remainingTurns = status.duration;
        else activeStatuses.Add(new ActiveStatusEffect {statusEffect = status, remainingTurns = status.duration});
    }
    public void RemoveStatus(StatusEffect status)
    {
        if(status == null) return;
        activeStatuses.RemoveAll(active => active.statusEffect == status);
    }
    public void ClearStatus()
    {
        activeStatuses.Clear();
    }
}