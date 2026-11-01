using UnityEngine;
public class DualSwords : WeaponEffect
{
[Range(0f, 1f)] public float defensePenalty = 0.2f;
[Range(0f, 1f)] public float offenseBonus = 0.2f;
[Range(0f, 1f)] public float momentumPerHit = 0.01f;
public int maxMomentumStacks = 10;
class Streak { public ICombatant target; public int stacks; }
public static DualSwords Find(Equipment equipment)
  {
    if(equipment == null || equipment.effects == null) return null;
    foreach(Effect effect in equipment.effects)
    if(effect is DualSwords dual) return dual;
    return null;
  }
  public override float OutgoingDamageMultiplier(ICombatant owner, HitContext hit) => IsActive(owner, hit) ? 1f + offenseBonus : 1f;
public override float StatMultiplier(ICombatant owner, Stat stat)
  {
    return stat == Stat.Defense || stat == Stat.Wisdom ? 1f - defensePenalty : 1f;
  }
  public override void OnHitResolved(ICombatant owner, HitContext hit, bool landed)
  {
    if(!IsActive(owner, hit)) return;
    Streak streak = BattleHooks.Get<Streak>(owner, this);
    if(streak == null) { streak = new Streak(); BattleHooks.Set(owner, this, streak); }
    if(!landed || streak.target != hit.defender) ClearStreak(owner, streak);
    if(!landed) return;
    streak.target = hit.defender;
    streak.stacks = Mathf.Min(streak.stacks + 1, maxMomentumStacks);
    float percent = streak.stacks * momentumPerHit;
    BattleHooks.SetMomentum(owner, owner, percent);
    BattleHooks.SetMomentum(hit.defender, owner, -percent);
  }
  public override void OnBattleEnd(ICombatant owner)
  {
    Streak streak = BattleHooks.Get<Streak>(owner, this);
    if(streak != null) ClearStreak(owner, streak);
  }
  void ClearStreak(ICombatant owner, Streak streak)
  {
    if(streak.target != null) BattleHooks.SetMomentum(streak.target, owner, 0f);
    BattleHooks.SetMomentum(owner, owner, 0f);
    streak.target = null;
    streak.stacks = 0;
  }
} 
