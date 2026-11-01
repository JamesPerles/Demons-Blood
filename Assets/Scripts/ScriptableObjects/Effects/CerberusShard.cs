using UnityEngine;
public class CerberusShard : BattleHookEffect
{
   [Range(0f, 1f)] public float allStatsBonus = 0.05f;
   public override float StatMultiplier(ICombatant owner, Stat stat) => 1f + allStatsBonus;
   public override int LimitDamage(ICombatant owner, int damage)
    {
        if(owner.currentHP <= 1) return damage;
        return Mathf.Min(damage, owner.currentHP - 1);
    }
} 
