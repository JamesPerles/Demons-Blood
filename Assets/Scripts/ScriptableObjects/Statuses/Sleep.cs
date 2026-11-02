using System.Collections;
using UnityEngine;
public class Sleep : StatusEffect
{
  public int enemyCritBonus = 50;
  public float critDamageMultiplier = 2f;
  public float allyDamageMultiplier = 0.9f;
  public Sleep() { statusName = "Sleep"; untilCured = true; }
  public override float StatMultiplier(ICombatant owner, Stat stat)
    {
        if(stat == Stat.Evasion || stat == Stat.Foresight || stat == Stat.Dodge) return 0f;
        return 1f;
    }
    public override int CritBonus(ICombatant owner, HitContext hit)
    {
        if(hit == null || hit.defender != owner) return 0;
        return SameSide(hit.attacker, owner) ? -1000 : enemyCritBonus;
    }
    public override float CritDamageMultiplier(ICombatant owner, HitContext hit)
    {
        return hit != null && hit.defender == owner ? critDamageMultiplier : 1f;
    }
    public override float IncomingDamageMultiplier(ICombatant owner, HitContext hit)
    {
        if(hit == null || hit.defender != owner || hit.attacker == owner || hit.redirected) return 1f;
        return SameSide(hit.attacker, owner) ? allyDamageMultiplier : 1f;
    }
    public override bool DisablesReactions(ICombatant owner) => true;
    public override bool PreventsPlanning(ICombatant owner) => true;
    public override IEnumerator ControlTurn(ICombatant owner, TurnControl control)
    {
        control.consumed = true;
        yield return Say($"{owner.currentName} is fast asleep.");
    }
    public override IEnumerator OnDamage(ICombatant owner, HitContext hit, int dealt)
    {
        if(dealt <= 0) yield break;
        BattleHooks.RemoveStatus(owner, this);
        BattleHooks.RemoveStatus(owner, this);
        yield return Say($"{owner.currentName} wakes up!");
    }
}
