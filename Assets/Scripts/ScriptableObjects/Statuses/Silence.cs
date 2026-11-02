using System.Collections;
using UnityEngine;
public class Silence : StatusEffect
{
   public int doomTurns = 3;
   public float windDamageMultiplier = 2f;
   public Silence() { statusName = "Silence";}
   public override float StatMultiplier(ICombatant owner, Stat stat) => stat == Stat.Speed ? 0f : 1f;
   public override float IncomingDamageMultiplier(ICombatant owner, HitContext hit)
    {
        return hit != null && hit.defender == owner && hit.AttackElement == Element.Wind ? windDamageMultiplier : 1f;
    }
    public override bool BlocksSpells(ICombatant owner) => true;
    public override IEnumerator OnDamage(ICombatant owner, HitContext hit, int dealt)
    {
        yield return CureByElement(owner, hit, dealt, Element.Wind);
    }
    public override IEnumerator OnTimer(object target)
    {
        ICombatant owner = target as ICombatant;
        ActiveStatusEffect active = Instance(owner);
        if(owner == null || active == null || owner.currentHP <= 0) yield break;
        if(BattleHooks.TurnNumber - active.appliedTurn < doomTurns) yield break;
        if(BattleHooks.IsInstantKillImmune(owner)) yield break;
        owner.currentHP = 0;
        if(BattleManager.instance != null) BattleManager.instance.UpdateUI();
        yield return Say($"{owner.currentName} suffocates!");
    }
}
