using System.Collections;
using UnityEngine;
public abstract class WeaponEffect : BattleHookEffect
{
 protected bool IsActive(ICombatant owner, HitContext hit)
  {
    return hit != null && hit.attacker == owner && hit.weapon != null 
    && hit.weapon.effects != null && hit.weapon.effects.Contains(this);
  }
  protected static Equipment WeaponOf(ICombatant who)
  {
    if(who is ActiveStats player) return player.BattleWeapon;
    if(who is EnemyAI enemy) return enemy.weaponSlot;
    return null;
  }
  protected static Equipment ShieldOf(ICombatant who)
  {
    if(who is ActiveStats player) return player.shieldSlot;
    if(who is EnemyAI enemy) return enemy.shieldSlot;
    return null;
  }
  protected bool Wielding(ICombatant owner)
  {
    Equipment weapon = WeaponOf(owner);
    return weapon != null && weapon.effects != null && weapon.effects.Contains(this);
  }
  public static bool Roll(float percent) => Random.Range(0f, 100f) < percent;
  protected static void ChangeStage(ICombatant who, Stat stat, int amount)
  {
    if(who is ActiveStats player) player.ChangeStatStage(stat, amount);
    else if(who is EnemyAI enemy) enemy.ChangeStatStage(stat, amount);
  }
  protected static bool ApplyStatusTo(ICombatant who, StatusEffect status, ICombatant source = null)
  {
    if(who is ActiveStats player) return player.ApplyStatus(status, source);
    if (who is EnemyAI enemy) return enemy.ApplyStatus(status, source);
    return false;
  }
  protected static int RestoreMP(ICombatant who, int amount)
  {
    if(who is ActiveStats player) { int before = player.currentMP; player.RestoreMP(amount); return player.currentMP - before; }
    if(who is EnemyAI enemy) { int before = enemy.currentMP; enemy.RestoreMP(amount); return enemy.currentMP - before; }
    return 0;
  }
} 
