using UnityEngine;
public class EyeForWeakness : BattleHookEffect
{
[Range(0f, 2f)] public float damageBonus = 0.25f;
[Range(0f, 1f)] public float damageReduction = 0.25f;
[Range(0f, 2f)] public float evasionBonus = 0.25f;
public override float OutgoingDamageMultiplier(ICombatant owner, HitContext hit)
    {
        return hit.attacker == owner && BattleHooks.IsAfflicted(hit.defender) ? 1f + damageBonus : 1f;
    }
    public override float IncomingDamageMultiplier(ICombatant owner, HitContext hit)
    {
        return hit.defender == owner && BattleHooks.IsAfflicted(hit.attacker) ? 1f - damageReduction : 1f;
    }
    public override float EvasionMultiplier(ICombatant owner, HitContext hit)
    {
        return hit.defender == owner && BattleHooks.IsAfflicted(hit.attacker) ? 1f + evasionBonus : 1f;
    }
} 
