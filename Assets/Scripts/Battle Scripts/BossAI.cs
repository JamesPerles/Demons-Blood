using System.Collections.Generic;
public class BossAI : EnemyAI
{
    public List<EnemyStats.EnemySpecialAttack> transformedSpecialAttacks = new List<EnemyStats.EnemySpecialAttack>();
    public override void CheckTransform()
    {
        bool wasTransformed = isTransformed;
        base.CheckTransform();
        if(isTransformed && !wasTransformed && transformedSpecialAttacks.Count > 0)
        {
            enemyStats.specialAttacks = transformedSpecialAttacks;
        }
    }
}