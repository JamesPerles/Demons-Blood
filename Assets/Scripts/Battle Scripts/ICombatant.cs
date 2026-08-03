using System.Collections.Generic;
public interface ICombatant
{
string currentName {get;}
int currentHP {get; set;}
int finalHP {get;}
int finalMP {get;}
int finalStrength {get;}
int finalMagic {get;}
int finalDefense {get;}
int finalWisdom {get;}
int finalTech {get;}
int finalAffinity {get;}
int finalSpeed {get;}
int finalLuck {get;}
int Accuracy {get;}
int Precision {get;}
int Evasion {get;}
int Foresight {get;}
int Critical{get;}
int Dodge {get;}
bool isDefending{get; set;}
Element currentMagicAffinity{get; set;}
List <ActiveStatusEffect> activeStatuses {get;}
List<Skill> learnedSkills {get;}
List<Skill> equippedSkills {get;}
void TakeDamage(int damage);
bool IsImmuneToStatus(StatusEffect status);
}
