using System.Collections;
using UnityEngine;
public class Frozen : StatusEffect
{
   public float fireDamageMultiplier = 2f;
   public float damageBonusPerHit = 0.1f;
   public float maxDamageBonus = 0.3f;
   public float statLossPerTurn = 0.05f;
   public float maxStatLoss = 0.3f;
   public Frozen() { statusName = "Frozen";}
   public override bool GearStatsDisabled(ICombatant owner) => true;
   public override float StatMultiplier(ICombatant owner, Stat stat)
    {
        if(stat == Stat.HP || stat == Stat.MP) return 1f;
        ActiveStatusEffect active = Instance(owner);
        int turns = active != null ? active.turnsActive : 0;
        return 1f - Mathf.Min(maxStatLoss, statLossPerTurn * turns);
    }
    public override float IncomingDamageMultiplier(ICombatant owner, HitContext hit)
    {
        if(hit == null || hit.defender != owner) return 1f;
        ActiveStatusEffect active = Instance(owner);
        int hits = active != null ? active.counter : 0;
        float multiplier = 1f + Mathf.Min(maxDamageBonus, damageBonusPerHit * hits);
        if(hit.AttackElement == Element.Fire) multilpier *= fireDamageMultiplier;
        return multiplier;
    }
    public override IEnumerator OnDamaged(ICombatant owner, HitContext hit, int dealt)
    {
        ActiveStatusEffect active = Instance(owner);
        if(active != null && dealt > 0) active.counter++;
        yield return CureByElement(owner, hit, dealt, Element.Fire);
    }
}
