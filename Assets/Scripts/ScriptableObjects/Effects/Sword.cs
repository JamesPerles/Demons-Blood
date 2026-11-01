using System.Collections;
using UnityEngine;
public class Sword : WeaponEffect
{
public float twoHandedMultiplier = 2f;
[Range(0f, 100)] public float counterChance = 100f;
public override int StrengthBonus(ICombatant owner, HitContext hit)
  {
    if(!IsActive(owner, hit) || hit.kind != HitKind.Physical) return 0;
    if(ShieldOf(owner) != null) return 0;
    return Mathf.RoundToInt(owner.finalStrength * (twoHandedMultiplier - 1f));
  }
  public override IEnumerator AfterHit(ICombatant owner, HitContext hit, bool landed)
  {
    if(landed || hit.blocked || hit.defender != owner) yield break;
    if(owner.currentHP <= 0 || hit.attacker == null || hit.attacker.currentHP <= 0) yield break;
    if(hit.kind != HitKind.Physical || Ranged.IsRanged(hit.weapon)) yield break;
    if(!Wielding(owner) || ShieldOf(owner) == null) yield break;
    if(BattleManager.instance == null || !Roll(counterChance)) yield break;
    yield return BattleManager.instance.CounterAttack(owner, hit.attacker);
  }
} 
