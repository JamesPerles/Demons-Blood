using System.Collections.Generic;
using UnityEngine;
public class ActiveStats : MonoBehaviour, ICombatant
{
    int maxHP;
    int maxMP;
    int currentStrength;
    int currentMagic;
    int currentDefense;
    int currentWisdom;
    int currentTech;
    int currentAffinity;
    int currentSpeed;
    int currentLuck;
    public int currentHP {get; set;}
    public int currentMP {get; set;}
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
    public int Accuracy {get; set;}
    public int Precision {get; set;}
    public int Evasion {get; set;}
    public int Foresight {get; set;}
    public int Critical {get; set;}
    public int Dodge {get; set;}
    public int currentExperience;
    public int currentExpToNextLevel;
    public int currentLevel;
    public string currentName {get; set;}
    public PlayerStats playerStats;
    public bool isDefending {get; set;}
    public Element currentMagicAffinity {get; set;}
    public Equipment weaponSlot;
    public Equipment headSlot;
    public Equipment bodySlot;
    public Equipment shieldSlot;
    public Equipment accessorySlot;
    public List<Spell> learnedSpells = new List<Spell>();
    public List<Art> learnedArts = new List<Art>();
    public List<Fusion> learnedFusions = new List<Fusion>();
    public List<Skill> learnedSkills {get; set;} = new List<Skill>();
    public List<ActiveStatusEffect> activeStatuses {get; set;} = new List<ActiveStatusEffect>();
    public Skill[] skillSlots = new Skill[6];
    public SkillTreeSet skillTrees;
    public int skillPoints = 0;
    List<int> treePoints = new List<int>();
    List<SkillTreePath> unlockedPaths = new List<SkillTreePath>();
    public List<StatusEffect> statusImmunities = new List<StatusEffect>();
    public List<BondProgress> bondProgress = new List<BondProgress>();
    public List<ActiveStats> currentBondPartners = new List<ActiveStats>();
    public float transformMultiplier = 1f;
    public float transformGauge = 0f;
    public float transformGaugeMax = 100f;
    public float gaugeGainPerTurn = 5f;
    public float gagueGainPerDamageDealt = 0.5f;
    public float gaugeGainPerDamageTaken = 0.5f;
    public bool isTransformed {get; private set;} = false;
    public bool transformUsedThisBattle {get; private set;} = false;
    int transformTurnsRemaining;
    public bool transformReady => !transformUsedThisBattle && !isTransformed && transformGauge >= transformGaugeMax;
    Dictionary<Stat, int> statStages = new Dictionary<Stat, int>();
    public int GetStatStage(Stat stat) => statStages.TryGetValue(stat, out int value) ? value : 0;
    public void ChangeStatStage(Stat stat, int amount)
    {
        int current = GetStatStage(stat);
        statStages[stat] = Mathf.Clamp(current + amount, StatStageUtility.MinStage, StatStageUtility.MaxStage);
        RecalculateStats();
    }
    public void ResetStatStages()
    {
        statStages.Clear();
        RecalculateStats();
    }
    public List<Skill> equippedSkills
    {
        get
        {
            List<Skill> equipped = new List<Skill>();
            foreach(Skill skill in skillSlots) if(skill != null) equipped.Add(skill);
            return equipped;
        }
    }
    public Skill GetSkillSlot(int slotIndex) => (slotIndex >= 0 && slotIndex < skillSlots.Length) ? skillSlots[slotIndex] : null;
    public bool SetSkillSlot(int slotIndex, Skill skill)
    {
        if(slotIndex < 0 || slotIndex >= skillSlots.Length) return false;
        bool isPersonalSlot = slotIndex == 0;
        if(skill != null && !isPersonalSlot && !learnedSkills.Contains(skill)) return false;
        skillSlots[slotIndex] = skill;
        return true;
    }
    public void ClearSkillSlot(int slotIndex)
    {
        if(slotIndex < 0 || slotIndex >= skillSlots.Length) return;
        skillSlots[slotIndex] = null;
    }
    void Awake()
    {
        currentName = playerStats.characterName;
        maxHP = playerStats.hp;
        maxMP = playerStats.mp;
        currentHP = maxHP;
        currentMP = maxMP;
        currentStrength = playerStats.strength;
        currentMagic = playerStats.magic;
        currentDefense = playerStats.defense;
        currentWisdom = playerStats.wisdom;
        currentTech = playerStats.tech;
        currentAffinity = playerStats.affinity;
        currentSpeed = playerStats.speed;
        currentLuck = playerStats.luck;
        currentExperience = playerStats.experience;
        currentExpToNextLevel = playerStats.expToNextLevel;
        currentLevel = playerStats.level;
        currentMagicAffinity = playerStats.magicAffinity;
        statusImmunities = new List<StatusEffect>(playerStats.immunities);
        if(playerStats.personalSkill != null) skillSlots[0] = playerStats.personalSkill;
        PlayerBondProgress();
        RecalculateStats();
    }
    void PlayerBondProgress()
    {
        bondProgress.Clear();
        foreach(BondData data in playerStats.bonds)
        {
            BondProgress progress = new BondProgress {partner = data.partner, points = 0};
            for (int i = 0; i < data.conversations.Count; i++) progress.conversationsViewed.Add(false);
            bondProgress.Add(progress);
        }
    }
    public BondProgress GetBondProgress(PlayerStats partner) => bondProgress.Find(entry => entry.partner == partner);
    public BondRank GetBondRank(PlayerStats partner)
    {
        BondProgress progress = GetBondProgress(partner);
        return progress != null ? BondSettings.GetRank(progress.points) : BondRank.None;
    }
    public void AddBondPoints(PlayerStats partner)
    {
        BondProgress progress = GetBondProgress(partner);
        if(progress != null) progress.points++;
    }
    public void SetBondPartners(List<ActiveStats> activeParty)
    {
        currentBondPartners = activeParty;
        RecalculateStats();
    }
    public bool IsImmuneToStatus(StatusEffect status) => status != null && statusImmunities.Contains(status);
    void UpdateUI()
    {
        if(BattleManager.instance != null) BattleManager.instance.UpdateUI();
    }
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
    void ApplyBondBonuses()
    {
        if(currentBondPartners == null) return;
        foreach(ActiveStats partner in currentBondPartners)
        {
            if(partner == this || partner == null) continue;
            BondData data = playerStats.bonds.Find(bond => bond.partner == partner.playerStats);
            if (data == null) continue;
            BondRank rank = GetBondRank(partner.playerStats);
            if(rank == BondRank.None) continue;
            BondRankBonus rankBonus = data.rankBonuses.Find(rb => rb.rank == rank);
            if (rankBonus == null) continue;
            foreach(StatBonus bonus in rankBonus.bonuses) ApplyStatBonus(bonus);
        }
    }
    void ApplyStatBonus(StatBonus bonus)
    {
        switch (bonus.stat)
        {
            case Stat.HP: finalHP += bonus.amount; break;
            case Stat.MP: finalMP += bonus.amount; break;
            case Stat.Strength: finalStrength += bonus.amount; break;
            case Stat.Magic: finalMagic += bonus.amount; break;
            case Stat.Defense: finalDefense += bonus.amount; break;
            case Stat.Wisdom: finalWisdom += bonus.amount; break;
            case Stat.Tech: finalTech += bonus.amount; break;
            case Stat.Affinity: finalAffinity += bonus.amount; break;
            case Stat.Speed: finalSpeed += bonus.amount; break;
            case Stat.Luck: finalLuck += bonus.amount; break;
            case Stat.Accuracy: Accuracy += bonus.amount; break;
            case Stat.Evasion: Evasion += bonus.amount; break;
            case Stat.Precision: Precision += bonus.amount; break;
            case Stat.Foresight: Foresight += bonus.amount; break;
            case Stat.Critical: Critical += bonus.amount; break;
            case Stat.Dodge: Dodge += bonus.amount; break;
        }
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
        ApplyBondBonuses();
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
        UpdateUI();
    }
    void ApplyStatGrowth()
    {
        if (Random.Range(0, 100) < playerStats.hpGrowth) maxHP += 1;
        if (Random.Range(0, 100) < playerStats.mpGrowth) maxMP += 1;
        if (Random.Range(0, 100) < playerStats.strengthGrowth) currentStrength += 1;
        if (Random.Range(0, 100) < playerStats.magicGrowth) currentMagic += 1;
        if (Random.Range(0, 100) < playerStats.defenseGrowth) currentDefense += 1;
        if (Random.Range(0, 100) < playerStats.wisdomGrowth) currentWisdom += 1;
        if (Random.Range(0, 100) < playerStats.techGrowth) currentTech += 1;
        if (Random.Range(0, 100) < playerStats.affinityGrowth) currentAffinity += 1;
        if (Random.Range(0, 100) < playerStats.speedGrowth) currentSpeed += 1;
        if (Random.Range(0, 100) < playerStats.luckGrowth) currentLuck += 1;
    }
    public void CheckLearnSet(int previousLevel, int newLevel)
    {
        foreach (Learnset learnset in playerStats.learnset)
        {
            if(learnset.level > previousLevel && learnset.level <= newLevel)
            Learnable(learnset.learnable);
        }
    }
    void Learnable(Learnable learnable)
    {
        if(learnable is Spell spell)
        {
            learnedSpells.Add(spell);
            Debug.Log($"{currentName} learned the spell {spell.spellName}!");
        }
        else if(learnable is Art art)
        {
            learnedArts.Add(art);
            Debug.Log($"{currentName} learned the art {art.artName}!");
        }
        else if(learnable is Fusion fusion)
        {
            learnedFusions.Add(fusion);
            Debug.Log($"{currentName} learned the fusion {fusion.fusionName}!");
        }
        else if(learnable is Skill skill)
        {
            learnedSkills.Add(skill);
            Debug.Log($"{currentName} learned the skill {skill.skillName}!");
        }
    }
        void TreePointsInitialized()
        {
            if(skillTrees == null) return;
            while(treePoints.Count < skillTrees.trees.Count) treePoints.Add(0);
        }
        public int GetTreePoints(int treeIndex)
    {
        TreePointsInitialized();
        return treeIndex >= 0 && treeIndex < treePoints.Count ? treePoints[treeIndex] : 0;
    }
    public bool SpendSkillPoint(int treeIndex)
    {
        if(skillTrees == null || treeIndex < 0 || treeIndex >= skillTrees.trees.Count) return false;
        if(skillPoints <= 0) return false;
        TreePointsInitialized();
        if(treePoints[treeIndex] >= 100) return false;
        skillPoints --;
        treePoints[treeIndex]++;
        CheckTreeLearnables(treeIndex);
        return true;
    }
    void CheckTreeLearnables(int treeIndex)
    {
        SkillTree tree = skillTrees.trees[treeIndex];
        int currentPoints = treePoints[treeIndex];
        foreach(SkillTreePath path in tree.paths)
        {
            if(path.pointsRequired <= currentPoints && !unlockedPaths.Contains(path))
            {
                unlockedPaths.Add(path);
                if(path.learnable != null) Learnable(path.learnable);
            }
        }
    }
    public bool IsPathUnlocked(SkillTreePath path) => unlockedPaths.Contains(path);
    public void GainExperience(int amount)
    {
        currentExperience += amount;
        CheckLevelUp();
        UpdateUI();
    }
    void CheckLevelUp()
    {
        while (currentExperience >= currentExpToNextLevel) LevelUp();
    }
    void LevelUp()
    {
        int previousLevel = currentLevel;
        currentExperience -= currentExpToNextLevel;
        currentLevel++;
        currentExpToNextLevel = Mathf.RoundToInt(currentExpToNextLevel * 1.2f);
        ApplyStatGrowth();
        skillPoints += 5;
        CheckLearnSet(previousLevel, currentLevel);
        UpdateUI();
    }
    public void Defend()
    {
        isDefending = true;
    }
    public void TakeDamage(int damage)
    {
        if (isDefending) 
        {
            damage = damage * 20/100; isDefending = false;
        }
        currentHP -= Mathf.Max(1, damage);
        currentHP = Mathf.Max(0, currentHP);
        UpdateUI();
    }
    public void Heal(int amount)
    {
        currentHP = Mathf.Min(currentHP + amount, finalHP);
        UpdateUI();
    }
    public void RestoreMP(int amount)
    {
        currentMP = Mathf.Min(currentMP + amount, finalMP);
        UpdateUI();
    }
    public Equipment Equip(Equipment newEquipment)
{
   if(newEquipment == null) return null;
   if(newEquipment.equipmentType == Equipment.EquipmentType.Weapon && 
   !playerStats.allowedWeaponTypes.Contains(newEquipment.weaponType)) return null;
   Equipment previous = null;
switch (newEquipment.equipmentType)
{
    case Equipment.EquipmentType.Weapon: previous = weaponSlot; weaponSlot = newEquipment; break;
    case Equipment.EquipmentType.Head: previous = headSlot; headSlot = newEquipment; break;
    case Equipment.EquipmentType.Body: previous = bodySlot; bodySlot = newEquipment; break;
    case Equipment.EquipmentType.Shield: previous = shieldSlot; shieldSlot = newEquipment; break;
    case Equipment.EquipmentType.Accessory: previous = accessorySlot; accessorySlot = newEquipment; break;
}
RecalculateStats(); return previous;
}
public void Unequip(Equipment.EquipmentType slotType)
    {
        switch(slotType)
        {
            case Equipment.EquipmentType.Weapon: weaponSlot = null; break;
            case Equipment.EquipmentType.Head: headSlot = null; break;
            case Equipment.EquipmentType.Body: bodySlot = null; break;
            case Equipment.EquipmentType.Shield: shieldSlot = null; break;
            case Equipment.EquipmentType.Accessory: accessorySlot = null; break;
        }
        RecalculateStats();
    }
    public Equipment GetEquipped(Equipment.EquipmentType slotType)
    {
        switch (slotType)
        {
            case Equipment.EquipmentType.Weapon: return weaponSlot;
            case Equipment.EquipmentType.Head: return headSlot;
            case Equipment.EquipmentType.Body: return bodySlot;
            case Equipment.EquipmentType.Shield: return shieldSlot;
            case Equipment.EquipmentType.Accessory: return accessorySlot;
            default: return null;
        }
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
    public void GaugeDamageDealt(int damage)
    {
        if(transformUsedThisBattle || isTransformed) return;
        transformGauge = Mathf.Min(transformGaugeMax, transformGauge + damage * gagueGainPerDamageDealt);
    }
    public void GaugeDamageTaken(int damage)
    {
        if(transformUsedThisBattle || isTransformed) return;
        transformGauge = Mathf.Min(transformGaugeMax, transformGauge + damage * gaugeGainPerDamageTaken);
    }
    public void GaugePerTurn()
    {
        if(transformUsedThisBattle || isTransformed) return;
        transformGauge = Mathf.Min(transformGaugeMax, transformGauge + gaugeGainPerTurn);
    }
    public void ActivateTransform()
    {
        if(!transformReady) return;
        isTransformed = true;
        transformUsedThisBattle = true;
        transformTurnsRemaining = 3;
        transformMultiplier = 1.2f;
        RecalculateStats();
    }
    public void TransformTimer()
    {
        if(!isTransformed) return;
        transformTurnsRemaining--;
        if(transformTurnsRemaining <= 0) EndTransform();
    }
    void EndTransform()
    {
        isTransformed = false;
        currentMP = 0;
        transformMultiplier = 1f /1.2f;
        RecalculateStats();
    }
    public void ResetTransformStat()
    {
        transformGauge = 0f;
        isTransformed = false;
        transformUsedThisBattle = false;
        transformTurnsRemaining = 0;
        transformMultiplier = 1f;
        RecalculateStats();
    }
    public List<int> SaveTreePoints() {TreePointsInitialized(); return new List<int> (treePoints);}
    public void LoadTreePoints(List<int> points) {treePoints = new List<int>(points);}
    public List<Vector2Int> SavePaths()
    {
            List<Vector2Int> result = new List<Vector2Int>();
            if(skillTrees == null) return result;
            for(int t = 0; t < skillTrees.trees.Count; t++)
            {
                SkillTree tree = skillTrees.trees[t];
                for(int n = 0; n < tree.paths.Count; n++)
                if(unlockedPaths.Contains(tree.paths[n])) result.Add(new Vector2Int(t, n));
            }
            return result;
        }
        public void LoadPaths(int treeIndex, int pathIndex)
    {
        if(skillTrees == null || treeIndex < 0 || treeIndex >= skillTrees.trees.Count) return;
        SkillTree tree = skillTrees.trees[treeIndex];
        if(pathIndex < 0 || pathIndex >= tree.paths.Count) return;
        SkillTreePath path = tree.paths[pathIndex];
        if(!unlockedPaths.Contains(path)) unlockedPaths.Add(path);
    }
        public void GrantLearnable(Learnable learnable) => Learnable(learnable);
        public void RefreshStats() => RecalculateStats();
    }


