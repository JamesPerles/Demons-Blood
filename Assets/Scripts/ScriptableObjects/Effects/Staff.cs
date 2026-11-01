using System.Collections;
using UnityEngine;
public class Staff : WeaponEffect
{
[Range(0f, 2f)] public float mpPerDamage = 1f;
public override IEnumerator AfterHit(ICombatant owner, HitContext hit, bool landed)
  {
    if(!landed || !IsActive(owner, hit) || hit.damageDealt <= 0) yield break;
    int restored = RestoreMP(owner, Mathf.RoundToInt(hit.damageDealt * mpPerDamage));
    if(restored > 0 && BattleTextBox.instance != null)
    yield return BattleTextBox.instance.ShowMessage($"{owner.currentName} recovers {restored} MP");
  }
} 
