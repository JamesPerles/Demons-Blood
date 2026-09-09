using UnityEngine;
using System.Collections;
[CreateAssetMenu(fileName = "New Race Damage", menuName = "Battle/Effects/Race Damage")]
public class RaceDamage : Effect
{
    public EnemyStats.Race targetRace = EnemyStats.Race.Unknown;
    public float multiplier = 2f;
public override IEnumerator Apply(object caster, object target)
    {
        yield break;
    }
}
