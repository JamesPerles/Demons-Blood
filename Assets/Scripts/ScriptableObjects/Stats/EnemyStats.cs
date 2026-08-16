using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "New Enemy", menuName = "Create New Enemy")]
public class EnemyStats : ScriptableObject
{
    public string enemyName;
    public int level = 1;
    public int hp = 0;
    public int mp = 0;
    public int strength = 0;
    public int magic = 0;
    public int defense = 0;
    public int wisdom = 0;
    public int tech = 0;
    public int affinity = 0;
    public int speed = 0;
    public int luck = 0;
    public int attackChance;
    public int defendChance;
    public int itemChance;
    public int runChance;
    public int increaseAttackChance;
    public int increaseDefendChance;
    public int increaseItemChance;
    public int increaseRunChance;
    public int hpthreshold;
     public int baseTargetWeight = 25;
    public int lowHPWeightBonus = 30;
    public int killWeightBonus = 50;
    public int defendingWeightBonus = 20;
    public int targetHPThreshold = 30;
    public int minGoldReward = 10;
    public int maxGoldReward = 35;
    public int minExpReward = 10;
    public int maxExpReward = 25;
    public float transformHPThreshold = 0f;
    public float bossTransformMultiplier = 1.3f;
    public Element magicAffinity;
    public List<StatusEffect> immunities = new List<StatusEffect>();
    public int enemyID;
    public string dexEntry;
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
     [System.Serializable]
    public class LootDrop
    {
        public Item item;
        public int quantity = 1;
        [Range(0, 100)] public int dropChance = 100;
    }
    public List<LootDrop> lootTable = new List<LootDrop>();
}
