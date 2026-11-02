using System.Collections;
using UnityEngine;
public class Deaf : StatusEffect
{
[Range(0f, 100f)] public float instantKillChance = 15f;
public float blockChanceMultiplier = 0.5f;
public float evasionMultiplier = 0.25f;
public float accuracyMultiplier = 0.85f;
public float magicMultiplier = 0.75f;
[Range(0f, 100f)] public float spellFailChance = 25f;
[Range(0f, 100f)] public float spellMisfireChance = 25f;
[Range(0f, 100f)] public float allyMissChance = 25f;
public Deaf() {statusName = "Deaf"; untilCured = true; }
public override float StatMultiplier(ICombatant owner, Stat stat)
    {
        switch(stat)
        {
            case Stat.Evasion: return evasionMultiplier;
            case Stat.Accuracy: return accuracyMultiplier;
            case Stat.Magic: return magicMultiplier;
            default: return 1f;
        }
    }
    public override float BlockChanceMultiplier(ICombatant owner, HitContext hit)
    {
        return hit != null && hit.defender == owner ? blockChanceMultiplier : 1f;
    }
    public override float InstantKillChance(ICombatant owner, HitContext hit)
    {
        return hit != null && hit.defender == owner ? instantKillChance : 0f;
    }
    public override float SpellFailChance(ICombatant owner) => spellFailChance;
    public override float SpellMisfireChance(ICombatant owner) => spellMisfireChance;
    public override float ExtraMissChance(ICombatant owner, HitContext hit)
    {
        if(hit == null || hit.attacker == owner || owner.currentHP <= 0) return 0f;
        return SameSide(hit.attacker, owner) ? allyMissChance : 0f;
    }
    public override bool PreventsPlanning(ICombatant owner) => owner is ActiveStats;
    public override IEnumerator ControlTurn(ICombatant owner, TurnControl control)
    {
        if(!(owner is ActiveStats) || BattleManager.instance == null) yield break;
        control.consumed = true;
        yield return Say($"{owner.currentName} can't hear anyone and acts on instinct.");
        yield return BattleManager.instance.AutoAct(owner, BattleManager.AutoMode.Sensible);
    }
 }
