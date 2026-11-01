using System.Collections.Generic;
using UnityEngine;
public class DarkPrince : BattleHookEffect
{
  public PlayerStats lucifer;
  public List<PlayerStats> siblings = new List<PlayerStats>();
  [Range(0f, 2f)] public float darkDamageBonus = 0.25f;
  public override float OutgoingDamageMultiplier(ICombatant owner, HitContext hit)
    {
        if(hit.AttackElement != Element.Dark) return 1f;
        ActiveStats ownerPlayer = owner as ActiveStats;
        ActiveStats attacker = hit.attacker as ActiveStats;
        if(ownerPlayer == null || attacker == null || lucifer == null) return 1f;
        if(ownerPlayer.playerStats != lucifer) return 1f;
        if(attacker == ownerPlayer) return 1f + darkDamageBonus;
        if(siblings.Contains(attacker.playerStats)) return 1f + darkDamageBonus;
        return 1f;
    }
} 
