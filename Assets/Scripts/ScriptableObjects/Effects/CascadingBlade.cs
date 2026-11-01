using System.Collections;
using UnityEngine;
public class CascadingBlade : BattleHookEffect
    {
        public PlayerStats lucifer;
        [Range(0f, 1f)] public float lowHpThreshold = 0.25f;
        public int lowHPCrit = 5;
        public int defeatedCrit = 10;
        public int luciferLowHpCrit = 10;
        public int luciferDefeatedCrit = 25;
        public override int CritBonus(ICombatant owner, HitContext hit)
    {
        if(hit.attacker != owner) return 0;
        int bonus = 0;
        foreach(ActiveStats ally in BattleHooks.PartyOf(owner))
        {
            if(ally == null || (ICombatant) ally == owner) continue;
            bool isLucifer = lucifer != null && ally.playerStats == lucifer;
            if(ally.currentHP <= 0) bonus += isLucifer ? luciferDefeatedCrit : defeatedCrit;
            else if(ally.finalHP > 0 && (float)ally.currentHP / ally.finalHP < 
            lowHpThreshold) bonus += isLucifer ? luciferLowHpCrit : lowHPCrit;
        }
        return bonus;
    }
    public override IEnumerator OnCrit(ICombatant owner, HitContext hit, int critStat)
    {
        if(hit.attacker != owner || owner.currentHP <= 0) yield break;
        int amount = Mathf.RoundToInt(owner.finalHP * Mathf.Max(0, critStat) / 100f);
        if(amount <= 0) yield break;
        int before = owner.currentHP;
        if(owner is ActiveStats player) player.Heal(amount);
        else if(owner is EnemyAI enemy) enemy.Heal(amount);
        int healed = owner.currentHP - before;
        if(healed > 0 && BattleTextBox.instance != null) yield return BattleTextBox.instance.ShowMessage($"{owner.currentName} recovers {healed} HP!");
    }
    }