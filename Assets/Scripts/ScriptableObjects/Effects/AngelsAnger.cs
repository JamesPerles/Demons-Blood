using System.Collections;
using UnityEngine;
public class AngelsAnger : BattleHookEffect
{
 public PlayerStats lucifer;
 [Range(0f, 1f)] public float lowHpThreshold = 0.25f;
 public int stageGain = 1;
 public int critBonus = 30;
 public bool includeSelf = false;
 bool CritReady(ICombatant owner)
    {
        int critTurn = BattleHooks.GetInt(owner, this);
        return critTurn > 0 && critTurn < BattleHooks.TurnNumber;
    }
    public override IEnumerator OnHeal(ICombatant owner, ICombatant healer, ICombatant target, int hpBefore, int healed)
    {
        if(healer != owner) yield break;
        ActiveStats amy = owner as ActiveStats;
        ActiveStats ally = target as ActiveStats;
        if(amy == null || ally == null) yield break;
        if(ally == amy && !includeSelf) yield break;
        if(ally.finalHP <= 0 || (float)hpBefore /ally.finalHP >= lowHpThreshold) yield break;
        amy.ChangeStatStage(Stat.Strength, stageGain);
        amy.ChangeStatStage(Stat.Magic, stageGain);
        if(BattleTextBox.instance != null) yield return BattleTextBox.instance.ShowMessage($"{amy.currentName}'s Angels Anger rises! Attack and Magic increase");
        if(lucifer != null && ally.playerStats == lucifer)
        {
            BattleHooks.SetInt(owner, this, BattleHooks.TurnNumber);
             if(BattleTextBox.instance != null) yield return BattleTextBox.instance.ShowMessage($"{amy.currentName}'s next attack is empowered!");
        }
       
    }
public override int CritBonus(ICombatant owner, HitContext hit)
    {
        return hit.attacker == owner && CritReady(owner) ? critBonus : 0;
    }
    public override IEnumerator OnActionEnd(ICombatant owner, ICombatant actor, bool attacked)
    {
        if(actor != owner) yield break;
        if(CritReady(owner)) BattleHooks.SetInt(owner, this, 0);
    }
} 
