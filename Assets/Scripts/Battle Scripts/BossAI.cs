using System.Collections.Generic;
public class BossAI : EnemyAI
{
    public List<EnemySpecialAttack> transformedSpecialAttacks = new List<EnemySpecialAttack>();
    public override void CheckTransform()
    {
        bool wasTransformed = isTransformed;
        base.CheckTransform();
        if(isTransformed && !wasTransformed && transformedSpecialAttacks.Count > 0)
        {
            specialAttacks = transformedSpecialAttacks;
        }
    }
}