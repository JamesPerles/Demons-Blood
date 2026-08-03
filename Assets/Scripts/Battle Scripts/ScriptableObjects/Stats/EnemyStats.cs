using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "New Enemy", menuName = "Create New Enemy")]
public class EnemyStats : ScriptableObject
{
    public string enemyName;
    public int level = 1;
    public int experienceGiven = 0;
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
    public Element magicAffinity;
    public List<StatusEffect> immunities = new List<StatusEffect>();
    public int enemyID;
    public string dexEntry;
}
