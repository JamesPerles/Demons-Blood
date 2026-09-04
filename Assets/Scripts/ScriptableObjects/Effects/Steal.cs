using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Steal : Effect
{
    [Range(0, 100)] public int stealChance = 50;
    public override IEnumerator Apply(object caster, object target)
    {
        ActiveStats casterStats = caster as ActiveStats;
        EnemyAI enemyTarget = target as EnemyAI;
        if(casterStats == null || enemyTarget == null)  yield break;
        List<EnemyStats.StealDrop> stealable = enemyTarget.enemyStats.stealTable.FindAll(drop => drop.item != null);
        if(stealable.Count == 0)
    {
        yield return BattleTextBox.instance.ShowMessage($"{casterStats.currentName} found nothing to steal on the enemy");
        yield break;
    } 
    int roll = Random.Range(0,100);
    if (roll < stealChance)
    {
        EnemyStats.StealDrop stolen = stealable[Random.Range(0, stealable.Count)];
        InventoryManager.instance.PickupItem(stolen.item);
        enemyTarget.enemyStats.stealTable.Remove(stolen);
        yield return BattleTextBox.instance.ShowMessage
        ($"{casterStats.currentName} stole {stolen.item.itemName} from {enemyTarget.currentName}!");
    }
    else
{
    yield return BattleTextBox.instance.ShowMessage($"{casterStats.currentName} failed to steal ");
    }
        } 
        }
