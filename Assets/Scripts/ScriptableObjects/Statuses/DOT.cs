using System.Collections;
using UnityEngine;
public abstract class DOT : StatusEffect
{
   public float dotPercentage = 1f / 16f;
    protected virtual float CurrentDotPercentage(ICombatant owner) => dotPercentage;
    public override IEnumerator OnTimer(object target)
    {
        ICombatant owner = target as ICombatant;
        if(owner == null || owner.currentHP <= 0) yield break;
        int damage = Mathf.Max(1, Mathf.RoundToInt(owner.finalHP * CurrentDotPercentage(owner)));
        int dealt = owner.TakeDamage(damage, true);
        if(BattleManager.instance != null) BattleManager.instance.UpdateUI();
        yield return Say($"{owner.currentName} takes {dealt} damage from {statusName}.");
    }
}
