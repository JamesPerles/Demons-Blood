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
    public List<EnemyStats.EnemySpecialAttack> currentSpecialAttacks;
    public List<EnemyStats.StealDrop> currentStealTable;
    public int currentHPThreshold;
    public int currentIncreaseAttackChance;
    public int currentIncreaseDefendChance;
    public int currentIncreaseItemChance;
    public int currentIncreaseRunChance;
    public EnemyStats enemyStats;
    public bool isDefending {get; set;}
    public Element currentMagicAffinity {get; set;}
    public List<StatusEffect> statusImmunities = new List<StatusEffect>();
    public bool immuneToInstantKill;
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
    public PersonalInventory personalInventory = new PersonalInventory();
    public List<Spell> learnedSpells = new List<Spell>();
    public List<Art> learnedArts = new List<Art>();
    public List<Fusion> learnedFusions = new List<Fusion>();
    public List<Skill> learnedSkills {get; set;} = new List<Skill>();
    public List<Skill> equippedSkills => learnedSkills;
    public List<ActiveStatusEffect> activeStatuses {get; set;} = new List<ActiveStatusEffect>();
    public float transformMultiplier = 1f;
    public bool isTransformed {get; protected set;} = false;
    StatCalculator statCalculator = new StatCalculator();
    public void SetMomentum(object source, float percent)
    {
        statCalculator.SetMomentum(source, percent);
        RecalculateStats();
    }
    public int GetStatStage(Stat stat) => statCalculator.GetStatStage(stat);
    public bool HasStatDebuff => statCalculator.HasDebuff();
    public void ChangeStatStage(Stat stat, int amount)
    {
        statCalculator.ChangeStatStage(stat, amount);
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
        currentSpecialAttacks = new List<EnemyStats.EnemySpecialAttack>(enemyStats.specialAttacks);
        currentStealTable = new List<EnemyStats.StealDrop>(enemyStats.stealTable);
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
    void RecalculateStats()
    {
        bool gearLocked = BattleHooks.GetStatsDisabled(this);
        StatCalculatorResult result = statCalculator.Recalculate(maxHP, maxMP, currentStrength, currentMagic,
        currentDefense, currentWisdom, currentTech, currentAffinity, currentSpeed, currentLuck,
        new[] {gearLocked ? null : weaponSlot, gearLocked ? null : headSlot, gearLocked ? null : bodySlot, gearLocked ? null : shieldSlot, accessorySlot }, transformMultiplier,
        flat => BattleHooks.FlatStats(this, flat), stat => BattleHooks.StatMultiplier(this, stat));
        finalHP = result.finalHP; finalMP = result.finalMP;
        finalStrength = result.finalStrength; finalMagic = result.finalMagic; finalDefense = result.finalDefense;
        finalWisdom = result.finalWisdom; finalTech = result.finalTech; finalAffinity = result.finalAffinity;
        finalSpeed = result.finalSpeed; finalLuck = result.finalLuck;
        Accuracy = result.accuracy; Precision = result.precision; Evasion = result.evasion;
        Foresight = result.foresight; Critical = result.critical; Dodge = result.dodge;
        if(currentHP > finalHP) currentHP = finalHP;
        if(currentMP > finalMP) currentMP = finalMP;
    }
    public enum EnemyActionType {Attack, Defend, Item, Run}
    public virtual EnemyStats.EnemySpecialAttack ChooseAttackMove()
    {
        List<EnemyStats.EnemySpecialAttack> usable = currentSpecialAttacks.FindAll(learnable => CanAfford(learnable.learnable));
        int totalWeight = enemyStats.basicAttackWeight;
        foreach (var move in usable) totalWeight += move.weight;
        if (totalWeight <= 0) return null;
        int roll = Random.Range(0, totalWeight);
        if (roll < enemyStats.basicAttackWeight) return null;
        roll -= enemyStats.basicAttackWeight;
        foreach (var move in usable)
        {
            if (roll < move.weight) return move;
            roll -= move.weight;
        } return null;
    }
    bool CanAfford(Learnable learnable)
    {
        if (learnable is Art art) return art.canKillUser || currentHP > art.Cost;
        if(learnable is Spell spell) return currentMP >= spell.Cost;
        if(learnable is Fusion fusion) return (fusion.canKillUser || currentHP > fusion.HPCost) && currentMP >= fusion.MPCost;
        return false;
    }
    public void PayCost(Learnable learnable)
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
        if(totalWeight <= 0) return EnemyActionType.Attack;
        int roll = Random.Range(0, totalWeight);
        if (roll < currentAttackChance) return EnemyActionType.Attack;
        roll -= currentAttackChance;
        if(roll < currentDefendChance) return EnemyActionType.Defend;
        roll -= currentDefendChance;
        if(roll < currentItemChance) return EnemyActionType.Item;
        roll -= currentItemChance;
        return EnemyActionType.Run;
    } 
       public ActiveStats ChooseTarget(List<ActiveStats> players)
    {
        if(players == null || players.Count == 0) return null;
        Dictionary<ActiveStats, int> weights = new Dictionary<ActiveStats, int>();
        foreach (ActiveStats player in players)
        {
            int weight = enemyStats.baseTargetWeight;
            bool isLowHP = player.currentHP <= (player.finalHP * enemyStats.targetHPThreshold / 100);
            if(isLowHP) weight += enemyStats.lowHPWeightBonus;
            int potentialDamage = Mathf.Max(1, finalStrength - player.finalDefense);
            bool wouldKill = player.currentHP <= potentialDamage;
            if(wouldKill) weight += enemyStats.killWeightBonus;
            if(player.isDefending) weight -= enemyStats.defendingWeightBonus;
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
    public bool aiChanged = false;
    public virtual void ChangeAI()
    {
        if(currentHP < currentHPThreshold && !aiChanged)
        {
            currentAttackChance += currentIncreaseAttackChance;
            currentDefendChance += currentIncreaseDefendChance;
            currentItemChance += currentIncreaseItemChance;
            currentRunChance += currentIncreaseRunChance;
            aiChanged = true;
        }
    }
    public void Defend()
    {
        isDefending = true;
    }
    public int TakeDamage(int damage, bool ignoreDefend = false)
    {
        if (isDefending && !ignoreDefend) damage = damage * 20/100;
        int final = BattleHooks.LimitDamage(this, Mathf.Max(1, damage));
        currentHP = Mathf.Max(0, currentHP - final);
        return final;
    }
    public void Heal(int amount)
    {
        currentHP = Mathf.Min(currentHP + amount, finalHP);
    }
    public void RestoreMP(int amount)
    {
        currentMP = Mathf.Min(currentMP + amount, finalMP);
    }
    public virtual void CheckTransform()
    {
        if(!isTransformed && enemyStats.transformHPThreshold > 0 && currentHP <= enemyStats.transformHPThreshold && currentHP > 0)
        {
            isTransformed = true;
            transformMultiplier = enemyStats.bossTransformMultiplier;
            RecalculateStats();
        }
    }
    public void ApplyStatus(StatusEffect status)
    {
        if(status == null) return;
        ActiveStatusEffect existing = activeStatuses.Find(active => active.statusEffect == status);
        if(existing != null) existing.remainingTurns = status.duration;
        if(activeStatuses.Count > 0)
        {
            if(!status.overrideOtherStatuses) return false;
            activeStatuses.Clear();
        }
        activeStatuses.Add(new ActiveStatusEffect {statusEffect = status, remainingTurns = status.duration, appliedTurn = BattleHooks.TurnNumber, source = source});
        RecalculateStats();
        return true;
    }
    public void RemoveStatus(StatusEffect status)
    {
        if(status == null) return;
        activeStatuses.RemoveAll(active => active.statusEffect == status);
        RecalculateStats();
    }
    public void ClearStatus()
    {
        activeStatuses.Clear();
    }
    public void RefreshStats() => RecalculateStats();
}