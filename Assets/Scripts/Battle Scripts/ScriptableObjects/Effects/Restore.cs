using System.Collections;
using UnityEngine;
public class Restore : Effect
{
    public int amount = 5;
    public override IEnumerator Apply(object caster, object target)
    {
        if (target is ActiveStats player) player.Heal(amount);
        else if (target is EnemyAI enemy) enemy.Heal(amount);
        yield break;
    }
}
