using UnityEngine;
public class Blind : StatusEffect
{
   public float accuracyMultiplier = 0.5f;
   public float precisionMultiplier = 0.5f;
   public float evasionMultiplier = 0.5f;
   public float foresightMultiplier = 0.5f;
   public float dodgeMultiplier = 0.5f;
   public float critMultiplier = 0f;
   public float baseHitMultiplier = 0.5f;
   public override float StatMultiplier(ICombatant owner, Stat stat)
    {
        switch(stat)
        {
            case Stat.Accuracy: return accuracyMultiplier;
            case Stat.Precision: return precisionMultiplier;
            case Stat.Evasion: return evasionMultiplier;
            case Stat.Foresight: return foresightMultiplier;
            case Stat.Dodge: return dodgeMultiplier;
            case Stat.Critical: return critMultiplier;
            default: return 1f;
        }
    }
    public override float BaseHitMultiplier(ICombatant owner, HitContext hit) => hit != null && hit.attacker == owner ? baseHitMultiplier : 1f;
}
