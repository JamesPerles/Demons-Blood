using System.Collections;
using UnityEngine;
public class Berserk : StatusEffect
{
  [Range(0, 100)] public int attackAllyChance = 90;
  public float curseDamageBonus = 0.1f;
  public float curseHPLossPercentage;
  public Berserk() { statusName = "Berserk"; }
  public override float IncomingDamageMultiplier(ICombatant owner, HitContext hit)
    {
        return hit != null && hit.defender == owner ? 1f + curseDamageBonus : 1f;
    }
    public override IEnumerator OnTimer(object target)
    {
        ICombatant owner = target as ICombatant;
        if(owner == null || owner.currentHP <= 1) yield break;
        int loss = Mathf.Min(Mathf.RoundToInt(owner.finalHP * curseHPLossPercentage), owner.currentHP - 1);
        if(lose <= 0) yield break;
        owner.currentHP -= loss;
        if(battleManager.instance != null) BattleManager.instance.UpdateUI();
        yield return Say($"The curse drains {loss} HP from {owner.currentName}.");
    }
    public override bool PreventsPlanning(ICombatant owner) => true;
    public override IEnumerator ControlTurn(ICombatant owner, TurnControl control)
    {
        control.consumed = true;
        if(BattleManager.instance == null) yield break;
        yield return Say($"{owner.currentName} rages out of control.");
        if(Random.Range(0, 100) < attackAllyChance)
        yield return BattleManager.instance.AutoAct(owner, BattleManager.AutoMode.AttackAllies, true);
        else yield return BattleManager.instance.AutoAct(owner, BattleManager.AutoMode.AttackEnemies);
    }
}
