using System.Collections;
using UnityEngine;
public class Cleared : StatusEffect
{
   public Cleared() { statusName = "Clear"; overridesOtherStatuses = true; }
   public override bool SwitchesOffEffects(ICombatant owner) => true;
   public override bool BlocksStatChanges(ICombatant owner) => true;
   public override IEnumerator OnApply(object target)
   {
      if(target is ActiveStats player) player.ResetStatStages();
      else if(target is EnemyAI enemy) enemy.ResetStatStages();
      yield break;
   }
}
