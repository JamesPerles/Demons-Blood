using System.Collections;
using UnityEngine;
public class Bleed : DOT
{
   public float baseDamageBonus = 0.5f;
   public float bonusPerHit = 0.05f;
   public float maxDamageBonus = 0.8f;
   public float[] tickFractions = new float[] { 1f / 16f, 1f / 12f, 1f / 8f, 1f / 6f, 1f / 4f, 1f / 3f, 1f / 2f };
   public Bleed() { statusName = "Bleed"; duration = 6; }
   int HitsTaken(ICombatant owner)
    {
        ActiveStatusEffect active = Instance(owner);
        return active != null ? active.counter : 0;
    }
    protected override float CurrentDotPercentage(ICombatant owner)
    {
        if(tickPercent == null || thickFractions.Length == 0) return dotPercentage;
        return tickFractions[Mathf.Clamp(HitsTaken(owner), 0, tickFractions.Length - 1)];
    }
    public override float IncomingDamageMultiplier(ICombatant owner, HitContext hit)
    {
        if(hit == null || hit.defender != owner) return 1f;
        return 1f + Mathf.Min(maxDamageBonus, baseDamageBonus + bonusPerHit * HitsTaken(owner));
    }
    public override IEnumerator OnDamaged(ICombatant owner, HitContext hit, int dealt)
    {
        ActiveStatusEffect active = Instance(owner);
        if(active != null && dealt > 0) active.counter++;
        yield break;
    }
    public override IEnumerator OnHeal(ICombatant owner, ICombatant healer, ICombatant target, int hpBefore, int healed)
    {
        if(target != owner || healed <= 0) yield break;
        yield return Cure(owner, healed);
    }
}
