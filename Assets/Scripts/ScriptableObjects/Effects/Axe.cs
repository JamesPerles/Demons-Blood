using UnityEngine;
public class Axe : WeaponEffect
{
  [Range(0f, 1f)] public float trueDamagePercent = 0.25f;
  public override int TrueDamage(ICombatant owner, HitContext hit)
  {
    if(!IsActive(owner, hit) || hit.kind != HitKind.Physical) return 0;
    return Mathf.RoundToInt(owner.finalStrength * trueDamagePercent);
  }
} 
