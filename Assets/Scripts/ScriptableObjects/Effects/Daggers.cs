using System.Collections;
using UnityEngine;
public class Daggers : WeaponEffect
{
[Range(0, 100)] public int startChance = 10;
[Range(0, 100)] public int chancePerHit = 5;
[Range(0, 100)] public int maxChance = 60;
public bool resetOnProc = true;
public override IEnumerator AfterHit(ICombatant owner, HitContext hit, bool landed)
  {
    if(!IsActive(owner, hit)) yield break;
    if(!landed) { BattleHooks.SetInt(owner, this, 0); yield break; }
    int streak = BattleHooks.GetInt(owner, this) + 1;
    ICombatant target = hit.defender;
    if(target != null && target.currentHP > 0)
    {
      int chance = Mathf.Min(maxChance, startChance + chancePerHit * (streak - 1));
      if(Roll(chance))
      {
        bool offense = Random.value < 0.5f;
        Stat first = offense ? Stat.Strength : Stat.Defense;
        Stat second = offense ? Stat.Magic : Stat.Wisdom;
        ChangeStage(target, first, -1);
        ChangeStage(target, second, -1);
        if(BattleTextBox.instance != null) yield return BattleTextBox.instance.ShowMessage
        ($"{target.currentName}'s {first} and {second} fell");
        if(resetOnProc) streak = 0;
      }
    }
    BattleHooks.SetInt(owner, this, streak);
  }
} 
