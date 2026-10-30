using System.Collections;
using UnityEngine;
[CreateAssetMenu(fileName = "New Restore", menuName = "Battle/Effects/Restore")]
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
