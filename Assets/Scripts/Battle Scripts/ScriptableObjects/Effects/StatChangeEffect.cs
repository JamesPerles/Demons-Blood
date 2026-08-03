using System.Collections;
using UnityEngine;
[CreateAssetMenu(fileName = "New Stat Change Effect", menuName = "Stat Effect")]
public class StatChangeEffect : Effect
{
public Stat stat;
public int stageChange = -1;
public override IEnumerator Apply(object caster, object target)
    {
        string name = null;
        if(target is ActiveStats player){player.ChangeStatStage(stat, stageChange); name = player.currentName;}
   if(target is EnemyAI enemy){enemy.ChangeStatStage(stat, stageChange); name = enemy.currentName; }
    if(name == null) yield break;
    string verb = stageChange >= 0 ? "increased" : "decreased";
    yield return BattleTextBox.instance.ShowMessage($"{name}'s {stat} {verb}!");
    }
}