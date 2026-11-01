using System.Collections;
using UnityEngine;
public class Lance : WeaponEffect
{
public int critPerStack = 5;
public int maxStacks = 10;
public bool resetWhenHit = true;
public override int CritBonus(ICombatant owner, HitContext hit)
  {
    return IsActive(owner, hit) ? BattleHooks.GetInt(owner, this) * critPerStack : 0;
  }
  public override IEnumerator AfterHit(ICombatant owner, HitContext hit, bool landed)
  {
    int stacks = BattleHooks.GetInt(owner, this);
    int updated = stacks;
    if(hit.attacker == owner && IsActive(owner, hit) && landed) updated++;
    else if(hit.defender == owner && Wielding(owner))
    {
      if(!landed && !hit.blocked) updated++;
      else if(landed && resetWhenHit) updated = 0;
    }
    updated = Mathf.Clamp(updated, 0, maxStacks);
    if(updated != stacks) BattleHooks.SetInt(owner, this, updated);
    yield break;
  }
} 
