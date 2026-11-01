using UnityEngine;
public class DualWield : WeaponEffect
{
 [Range(0.1f, 1f)] public float damagePerHit = 0.6f;
 public bool blocksShield = true;
 public static DualWield Find(Equipment weapon)
  {
    if(weapon == null || weapon.effects == null) return null;
    foreach(Effect effect in weapon.effects)
    if(effect is DualWield dual) return dual;
    return null;
  }
  public override int ExtraHits(ICombatant owner, HitContext hit) => IsActive(owner, hit) ? 1 : 0;
  public override float HitDamageScale(ICombatant owner, HitContext hit) => IsActive(owner, hit) ? damagePerHit : 1f;
} 
