using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "New Character", menuName = "Create New Character")]
public class PlayerStats : ScriptableObject
{
    public string characterName;
    public int level = 1;
    public int experience = 0;
    public int expToNextLevel = 100;
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
    public int hpGrowth = 0;
    public int mpGrowth = 0;
    public int strengthGrowth = 0;
    public int magicGrowth = 0;
    public int defenseGrowth = 0;
    public int wisdomGrowth = 0;
    public int techGrowth = 0;
    public int affinityGrowth = 0;
    public int speedGrowth = 0;
    public int luckGrowth = 0;
    public Learnset[] learnset;
    public Element magicAffinity;
    public List<BondData> bonds = new List<BondData>();
    public List<StatusEffect> immunities = new List<StatusEffect>();
    public List<Equipment.WeaponType> allowedWeaponTypes = new List<Equipment.WeaponType>();
    public Skill personalSkill;
    public SkillTreeSet skillTrees;
    [TextArea(3, 10)] public string bio;
    public string sex;
    public string sexuality;
    public string race;
    public string from;
    public List<string> likes = new List<string>();
    public List<string> dislikes = new List<string>();
}
