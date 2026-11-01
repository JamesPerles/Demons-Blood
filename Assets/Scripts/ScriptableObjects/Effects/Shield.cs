using UnityEngine;
public class Shield : WeaponEffect
{
[Range(0f, 100f)] public float blockChance = 25f;
[Range(0f, 100f)] public float deflectChance = 25f;
public bool blocksMagic = false;
public override BlockResult TryBlock(ICombatant owner, HitContext hit)
  {
    if(hit == null || hit.defender != owner) return BlockResult.None;
    if(hit.kind != HitKind.Physical && !blocksMagic) return BlockResult.None;
    if(!Roll(blockChance)) return BlockResult.None;
    return Roll(deflectChance) ? BlockResult.Deflected : BlockResult.Blocked;
  }
} 
