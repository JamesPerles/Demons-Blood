using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class StatusResult { public bool applied; }
public abstract class StatusEffect : BattleHookEffect
{
    public string statusName;
    public bool untilCured { get => true; set { } }
    public bool overridesOtherStatuses = false;
    public const float minCureChance = 25;
    public const float maxCureChance = 50f;
    public virtual IEnumerator OnApply(object target) {yield break;}
    public virtual IEnumerator OnTimer(object target) {yield break;}
    public virtual IEnumerator OnExpire(object target) {yield break;}
    protected ActiveStatusEffect Instance(ICombatant owner)
    {
        if(owner == null || owner.activeStatuses == null) return null;
        return owner.activeStatuses.Find(active => active.statusEffect == this);
    }
    protected static bool SameSide(ICombatant first, ICombatant second)
    {
        return (first is ActiveStats && second is ActiveStats) || (first is EnemyAI && second is EnemyAI);
    }
    public static bool Roll(float percent) => Random.Range(0f, 100f) < percent;
    protected static IEnumerator Say(string message)
    {
        if(BattleTextBox.instance != null) yield return BattleTextBox.instance.ShowMessage(message);
    }
    protected IEnumerator Cure(ICombatant owner, int amount)
    {
        if(owner == null || amount <= 0) yield break;
        float percent = owner.finalHP > 0 ? 100f * amount / owner.finalHP : 100f;
        float chance = Mathf.Clamp(percent, minCureChance, maxCureChance);
        if(Random.Range(0f, 100) >= chance) yield break;
        BattleHooks.RemoveStatus(owner, this);
        yield return Say($"{owner.currentName} is cured of {statusName}!");
    }
    protected IEnumerator CureByElement(ICombatant owner, HitContext hit, int dealt, params Element[] curingElements)
    {
        if(hit == null || dealt <= 0 || owner.currentHP <= 0) yield break;
        if(System.Array.IndexOf(curingElements, hit.AttackElement) < 0) yield break;
        yield return Cure(owner, dealt);
    }
}
[System.Serializable]
public class ActiveStatusEffect
{
    public StatusEffect statusEffect;
    public int remainingTurns;
    public int turnsActive;
    public int counter;
    public int appliedTurn;
    [System.NonSerialized] public ICombatant source;
}
[CreateAssetMenu(fileName = "New Status Effect", menuName = "Battle/Effects/Inflict Status")]
public class InflictStatusEffect : Effect
{
    public StatusEffect statustoInflict;
    [Range(0, 100)] public int chanceToInflict = 100;
    public override IEnumerator Apply(object caster, object target)
    {
        yield return TryInflict(caster, target, statustoInflict, chanceToInflict, new StatusResult(), true);
    }
    public static IEnumerator TryInflict(object caster, object target, StatusEffect status, int baseChance, StatusResult result, bool announceFailure)
    {
        ICombatant victim = target as ICombatant;
        if(status == null || victim == null) yield break;
        if(victim.IsImmuneToStatus(status))
        {
            if(announceFailure && BattleTextBox.instance != null) yield return BattleTextBox.instance.ShowMessage($"{victim.currentName} is immune to {status.statusName}.");
            yield break;
        }
        int finalChance = Mathf.Clamp(baseChance + BattleHooks.StatusChanceBonus(caster as ICombatant, victim, status), 0, 100);
        if(Random.Range(0, 100) >= finalChance) yield break;
        if(!BattleHooks.TryApplyStatus(victim, status, caster as ICombatant))
        {
           if(announceFailure && BattleTextBox.instance != null) yield return BattleTextBox.instance.ShowMessage($"{victim.currentName} can't be afflicted with {status.statusName} right now.");
            yield break;
        }
        if(result != null) result.applied = true;
        yield return status.OnApply(target);
       if(BattleTextBox.instance != null) yield return BattleTextBox.instance.ShowMessage($"{victim.currentName} is afflicted with {status.statusName}");
    }
}
[CreateAssetMenu(fileName = "Cure Status Eff", menuName = "Battle/Effects/CureStatus")]
public class CureStatusEffect : Effect
{
    public StatusEffect statusToCure;
  public override IEnumerator Apply(object caster, object target)
    {
        string name = null;
    if(target is ActiveStats player)
        {
            if(statusToCure == null) player.ClearStatus(); else player.RemoveStatus(statusToCure);
      name = player.currentName;
        }
    else if(target is EnemyAI enemy)
        {
            if(statusToCure == null) enemy.ClearStatus(); else enemy.RemoveStatus(statusToCure);
      name = enemy.currentName;
        }
    string message = statusToCure == null ? $"{name}'s status ailments were cured" : $"{name} was cured of {statusToCure.statusName}.";
    if(BattleTextBox.instance != null) yield return BattleTextBox.instance.ShowMessage(message);
}
}