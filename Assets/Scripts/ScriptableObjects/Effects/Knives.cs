using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Knives : WeaponEffect
{
[Range(0, 100)] public int statusChance = 10;
[Range(0, 100)] public int debuffChance = 10;
public List<StatusEffect> possibleStatuses = new List<StatusEffect>();
public List<Stat> possibleDebuffs = new List<Stat> {Stat.Strength, Stat.Magic, Stat.Defense, Stat.Wisdom,
Stat.Speed, Stat.Accuracy, Stat.Evasion};
public override IEnumerator AfterHit(ICombatant owner, HitContext hit, bool landed)
  {
    if(!landed || !IsActive(owner, hit)) yield break;
    ICombatant target = hit.defender;
    if(target == null || target.currentHP <= 0) yield break;
    if(possibleStatuses.Count > 0)
    {
      StatusEffect status = possibleStatuses[Random.Range(0, possibleStatuses.Count)];
      if(status != null && !target.IsImmuneToStatus(status))
      {
        int chance = Mathf.Clamp(statusChance + BattleHooks.StatusChanceBonus(owner, target, status), 0, 100);
        if(Roll(chance))
        {
          ApplyStatusTo(target, status);
          yield return status.OnApply(target);
          if(BattleTextBox.instance != null) yield return BattleTextBox.instance.ShowMessage
          ($"{target.currentName} is afflicted with {status.statusName}");
        }
      }
    }
    if(possibleDebuffs.Count > 0 && Roll(debuffChance))
    {
      Stat stat = possibleDebuffs[Random.Range(0, possibleDebuffs.Count)];
      ChangeStage(target, stat, -1);
      if(BattleTextBox.instance != null) yield return BattleTextBox.instance.ShowMessage($"{target.currentName}'s {stat} fell");
    }
  }
} 
