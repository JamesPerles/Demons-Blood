using System.Collections;
using UnityEngine;
public class Paralysis : StatusEffect
{
    public float statMultiplier = 2f / 3f;
    public Paralysis() { statusName = "Paralysis"; }
    public override float StatMultiplier(ICombatant owner, Stat stat)
    {
        return stat == Stat.Strength || stat == Stat.Defense || stat == Stat.Speed ? statMultiplier : 1f;
    }
    public override string BlockAttackReason(ICombatant owner, HitKind kind, Equipment weapon)
    {
        if(kind == HitKind.Magic || Ranged.IsRanged(weapon)) return null;
        return $"{owner.currentName} is paralyzed and can't move.";
    }
    public override IEnumerator OnDamaged(ICombatant owner, HitContext hit, int dealt)
    {
        yield return CureByElement(owner, hit, dealt, Element.Earth);
    }
}
