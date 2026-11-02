using System.Collections;
using UnityEngine;
public abstract class WeakenDot : DOT
{
 [Range(0f, 1f)] public float startReduction = 0.5f;
 [Range(0f, 1f)] public float reductionPerTurn = 0.05f;
 [Range(0f, 1f)] public float maxReduction = 0.8f;
 protected abstract float OutputWeight(HitKind kind);
 protected abstract Element[] CuringElements { get; }
 protected float CurrentReduction(ICombatant owner)
    {
            ActiveStatusEffect active = Instance(owner);
            int turns = active != null ? active.turnsActive : 0;
            return Mathf.Min(maxReduction, startReduction + reductionPerTurn * turns);
        }
    public override float OutgoingDamageMultiplier(ICombatant owner, HitContext hit)
    {
        if(hit == null || hit.attacker != owner) return 1f;
        float weight = OutputWeight(hit.kind);
        if(weight <= 0f) return 1f;
        return Mathf.Max(0f, 1f - CurrentReduction(owner) * weight);
    }
        public override IEnumerator OnDamaged(ICombatant owner, HitContext hit, int dealt)
    {
        yield return CureByElement(owner, hit, dealt, CuringElements);
    }

}
