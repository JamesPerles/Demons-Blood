using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class StatusResult { public bool applied; }
public abstract class StatusEffect : BattleHookEffect
{
    public string statusName;
    public int duration = 3;
    public bool cured = false;
    public bool overrideStatuses = false;
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
    public int appliedTUrn;
    [System.NonSerialized] public ICombatant source;
}
[CreateAssetMenu(fileName = "New Status Effect", menuName = "Battle/Effects/Inflict Status")]
public class InflictStatusEffect : Effect
{
    public StatusEffect statustoInflict;
    [Range(0, 100)] public int chanceToInflict = 100;
    public override IEnumerator Apply(object caster, object target)
    {
        if (statustoInflict == null) yield break;
        string name = null;
        bool immune = false;
        if(target is ActiveStats player) {name = player.currentName; immune = player.IsImmuneToStatus(statustoInflict);}
        else if (target is EnemyAI enemy) {name = enemy.currentName; immune = enemy.IsImmuneToStatus(statustoInflict);} 
        if(immune)
        {
           if(BattleTextBox.instance != null) yield return BattleTextBox.instance.ShowMessage($"{name} is immune to {statustoInflict.statusName}.");
            yield break;
        }
        int roll = Random.Range(0, 100);
        int finalChance = Mathf.Clamp(chanceToInflict + BattleHooks.StatusChanceBonus(caster as ICombatant, target as ICombatant, statustoInflict), 0, 100);
        if(roll >= finalChance) yield break;
        if(target is ActiveStats plyr) plyr.ApplyStatus(statustoInflict);
        else if (target is EnemyAI enmy) enmy.ApplyStatus(statustoInflict);
        yield return statustoInflict.OnApply(target);
       if(BattleTextBox.instance != null) yield return BattleTextBox.instance.ShowMessage($"{name} is afflicted with {statustoInflict.statusName}");
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