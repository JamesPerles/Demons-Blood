using UnityEngine;
public class HotHeaded : BattleHookEffect
{
 [Range(0f, 2f)] public float damageBonus = 0.15f;
 public StatusEffect burnStatus;
 public int burnChanceBonus = 30;
 public bool fastestBonus = true;
 bool ActedFirst(ICombatant owner) => BattleHooks.FirstActor == owner;
 public override float OutgoingDamageMultiplier(ICombatant owner, HitContext hit)
    {
        if(hit.attacker != owner || !ActedFirst(owner)) return 1f;
        return 1f + damageBonus;
    }
    public override int StatusChanceBonus(ICombatant owner, ICombatant caster, ICombatant target, StatusEffect status)
    {
        if(caster != owner || burnStatus == null || status != burnStatus) return 0;
        if(fastestBonus && !ActedFirst(owner)) return 0;
        return burnChanceBonus;
    }
}
