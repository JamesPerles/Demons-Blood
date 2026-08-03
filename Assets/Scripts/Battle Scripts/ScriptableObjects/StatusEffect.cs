using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public abstract class StatusEffect : ScriptableObject
{
    public string statusName;
    public string description;
    public int duration = 3;
    public virtual IEnumerator OnApply(object target) {yield break;}
    public abstract IEnumerator OnTimer(object target);
    public virtual IEnumerator OnExpire(object target) {yield break;}
}
[System.Serializable]
public class ActiveStatusEffect
{
    public StatusEffect statusEffect;
    public int remainingTurns;
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
            yield return BattleTextBox.instance.ShowMessage($"{name} is immune to {statustoInflict.statusName}.");
            yield break;
        }
        int roll = Random.Range(0, 100);
        if(roll >= chanceToInflict) yield break;
        if(target is ActiveStats plyr) plyr.ApplyStatus(statustoInflict);
        else if (target is EnemyAI enmy) enmy.ApplyStatus(statustoInflict);
       yield return BattleTextBox.instance.ShowMessage($"{name} is afflicted with {statustoInflict.statusName}");
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
    yield return BattleTextBox.instance.ShowMessage(message);
}
}