using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Steal : Effect
{
    public override IEnumerator Apply(object caster, object target)
    {
        ActiveStats casterStats = caster as ActiveStats;
        EnemyAI enemyTarget = target as EnemyAI;
        if(casterStats == null || enemyTarget == null)  yield break;
        List<EnemyStats.StealDrop> stealable = enemyTarget.currentStealTable.FindAll(drop => drop.item != null);
        if(stealable.Count == 0)
    {
        yield return BattleTextBox.instance.ShowMessage($"{casterStats.currentName} found nothing to steal on the enemy");
        yield break;
    } 
    List<EnemyStats.StealDrop> successful = new List<EnemyStats.StealDrop>();
    foreach(EnemyStats.StealDrop drop in stealable)
    if(Random.Range(0, 100) < drop.stealChance) successful.Add(drop);
    if (successful.Count == 0)
    {
        yield return BattleTextBox.instance.ShowMessage($"{casterStats.currentName} failed to steal ");
        yield break;
    }
        EnemyStats.StealDrop stolen = successful[0];
        foreach(EnemyStats.StealDrop drop in successful)
        if(drop.stealChance < stolen.stealChance) stolen = drop;
        InventoryManager.instance.PickupItem(stolen.item);
        enemyTarget.currentStealTable.Remove(stolen);
        yield return BattleTextBox.instance.ShowMessage
        ($"{casterStats.currentName} stole {stolen.item.itemName} from {enemyTarget.currentName}!");
    }
} 
