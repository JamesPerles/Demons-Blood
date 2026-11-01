using System.Collections;
using UnityEngine;
[CreateAssetMenu(fileName = "New Restore", menuName = "Battle/Effects/Restore")]
public class Restore : Effect
{
    public int amount = 5;
    public override IEnumerator Apply(object caster, object target)
    {
        ICombatant receiver = target as ICombatant;
        int hpBefore = receiver != null ? receiver.currentHP : 0;
        if (target is ActiveStats player) player.Heal(amount);
        else if (target is EnemyAI enemy) enemy.Heal(amount);
        if(receiver != null)
        {
            int healed = receiver.currentHP - hpBefore;
            if(healed > 0 && BattleTextBox.instance != null) yield return BattleTextBox.instance.ShowMessage($"{receiver.currentName} recovers {healed} HP");
            if(healed > 0) yield return BattleHooks.OnHeal(caster as ICombatant, receiver, hpBefore, healed);
        }
    }
}
