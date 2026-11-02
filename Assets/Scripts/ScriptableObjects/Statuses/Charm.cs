using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Charm : StatusEffect
{
   [Range(0f, 100)] public float allyDistractChance = 25f;
   [Range(0f, 100f)] public float stopAllyChance = 50f;
   [Range(0f, 100f)] public float spreadChance = 50f;
   [Range(0f, 100f)] public float enemyPullChance = 15f;
   [Range(0f, 100f)] public float friendlyAoeChance = 25f;
   public float maxHPMultiplier = 0.5f;
   public float maxMPMultiplier = 0.5f;
   public Charm() { statusName = "Charm"; }
   public override float StatMultiplier(ICombatant owner, Stat stat)
    {
        if(stat == Stat.HP) return maxHPMultiplier;
        if(stat == Stat.MP) return maxMPMultiplier;
        return 1f;
    }
    static string SexOf(string text)
    {
        string sex = (text ?? "").Trim().ToLowerInvariant();
        if(sex.StartsWith("f")) return "f";
        if(sex.StartsWith("m")) return "m";
    }
    public static bool AttractedTo(ActiveStats viewer, ActiveStats other)
    {
        string sexuality = (viewer.currentSexuality ?? "").ToLowerInvariant();
        if(sexuality.Contains("bi")) return true;
        string mine = SexOf(viewer.currentSex);
        string theirs = SexOf(other.currentSex);
        if(mine == "" || theirs == "") return false;
        if(sexuality.Contains("gay")) return mine = theirs;
        if(sexuality.Contains("straight")) return mine != theirs;
        return false;
    }
    public override IEnumerator ControlTurn(ICombatant owner, TurnControl control)
    {
        ActiveStatusEffect active = Instance(owner);
        ICombatant source = active != null ? active.source : null;
        if(owner is ActiveStats charmed)
        {
            bool distracted = false;
            foreach(ActiveStats ally in BattleHooks.PartyOf(owner).ToArray())
            {
                if(ally == charmed || ally.currentHP <= 0 || !AttractedTo(charmed, ally)) continue;
                if(!Roll(allyDistractChance)) continue;
                distracted = true;
                yield return Say($"{charmed.currentName} can't take their eyes off {ally.currentName}.");
                if(!Roll(stopAllyChance)) continue;
                BattleHooks.SetInt(ally, BattleHooks.DistractedKey, BattleHooks.TurnNumber);
                yield return Say($"{ally.currentName} is distracted by the attention.");
                if(Roll(spreadChance) && BattleHooks.TryApplyStatus(ally, this, source))
                yield return Say($"{ally.currentName} is charmed too!");
            }
            if(distracted)
            {
                control.consumed = true;
                yield break;
            }
            if(BattleManager.instance == null || !Roll(enemyPullChance)) yield break;
            control.consumed = true;
            if(source != null && source.currentHP > 0)
            {
                yield return Say($"{owner.currentName} is swept away by {source.currentName}'s charm!");
                bool wide = Roll(friendlyAoeChance);
                yield return BattleManager.instance.AutoAct(owner, BattleManager.AutoMode.AttackAllies, false, wide);
            }
            else
            {
                yield return Say($"{owner.currentName} loses all sense and acts wildly!");
                yield return BattleManager.instance.AutoAct(owner, BattleManager.AutoMode.Chaos);
            }
        }
    }
}
