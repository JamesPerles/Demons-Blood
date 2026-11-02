using System.Collections;
using UnityEngine;
public class Slow : StatusEffect
{
    public float speedMultiplier = 0.5f;
    public float accuracyMultiplier = 0.75f;
    public float evasionMultiplier = 0.75f;
    public float dodgeMultiplier = 0.75f;
    public float lightningDamageMultiplier = 2f;
    public Slow() { statusName = "Slow";}
    public override float StatMultiplier(ICombatant owner, Stat stat)
    {
        switch(stat)
        {
            case Stat.Speed: return speedMultiplier;
            case Stat.Accuracy: return accuracyMultiplier;
            case Stat.Evasion: return evasionMultiplier;
            case Stat.Dodge: return dodgeMultiplier;
            default: return 1f;
        }
    }
    bool IsUsingLightning(ICombatant owner, HitContext hit) => hit != null && hit.defender == owner && hit.AttackElement == Element.Lightning;
    public override float IncomingDamageMultiplier(ICombatant owner, HitContext hit) => IsUsingLightning(owner, hit) ? lightningDamageMultiplier : 1f;
    public override bool IgnoresDefend(ICombatant owner, HitContext hit) => IsLightningAtMe(owner, hit);
    public override bool IgnoresDefenseStats(ICombatant owner, HitContext hit) => IsLightningAtMe(owner, hit);
    public override bool AttracksLightning(ICombatant owner) => true;
    public override IEnumerator OnDamaged(ICombatant owner, HitContext hit, int dealt)
    {
        yield return CureByElement(owner, hit, dealt, Element.Fire, Element.Wind);
    }
}
