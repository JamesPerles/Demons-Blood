using UnityEngine;
public class ForgeMaster : BattleHookEffect
{
  public int bonusPerEnhanceLevel = 2;
  public float elementMultiplierFactor = 2f;
  public override void ModifyFlatStats(ICombatant owner, StatCalculatorResult result)
    {
        ActiveStats player = owner as ActiveStats;
        Equipment weapon = player != null ? player.BattleWeapon : null;
        if(weapon == null) return;
        result.finalStrength += weapon.enhancementLevel * bonusPerEnhanceLevel;
    }
    public override float ModifyElementMultiplier(ICombatant owner, HitContext hit, Element element, float multiplier)
    {
        if(hit.attacker != owner) return multiplier;
        Equipment weapon = hit.weapon;
        if(weapon == null || weapon.element == Element.None || element != weapon.element) return multiplier;
        if(multiplier <= 1f) return multiplier;
        return multiplier * elementMultiplierFactor;
    }
} 
