using System.Collections;
using UnityEngine;
public class BattleHookEffect : Effect
{
   public override IEnumerator Apply(object caster, object target) {yield break; }
   public virtual float OutgoingDamageMultiplier(ICombatant owner, HitContext hit) => 1f;
   public virtual float IncomingDamageMultiplier(ICombatant owner, HitContext hit) => 1f;
   public virtual int CritBonus(ICombatant owner, HitContext hit) => 0;
   public virtual float EvasionMultiplier(ICombatant owner, HitContext hit) => 1f;
   public virtual float ModifyElementMultiplier(ICombatant owner, HitContext hit, Element element, float multiplier) => multiplier;
   public virtual int StatusChanceBonus(ICombatant owner, ICombatant caster, ICombatant target, StatusEffect status) => 0;
   public virtual int LimitDamage(ICombatant owner, int damage) => damage;
   public virtual void ModifyFlatStats(ICombatant owner, StatCalculatorResult result) { }
   public virtual float StatMultiplier(ICombatant owner, Stat stat) => 1f;
   public virtual bool GrantsSecondSpell(ICombatant owner) => false;
   public virtual int ExtraHits(ICombatant owner, HitContext hit) => 0;
   public virtual float HitDamageScale(ICombatant owner, HitContext hit) => 1f;
   public virtual int StrengthBonus(ICombatant owner, HitContext hit) => 0;
   public virtual void OnHitResolved(ICombatant owner, HitContext hit, bool landed) { }
   public virtual void OnBattleEnd(ICombatant owner) { }
   public virtual bool IgnoresDefend(ICombatant owner, HitContext hit) => false;
   public virtual int TrueDamage(ICombatant owner, HitContext hit) => 0;
   public virtual BlockResult TryBlock(ICombatant owner, HitContext hit) => BlockResult.None;
   public virtual IEnumerator AfterHit(ICombatant owner, HitContext hit, bool landed) { yield break; }
   public virtual IEnumerator OnCrit(ICombatant owner, HitContext hit, int critStat) { yield break; }
   public virtual IEnumerator OnHeal(ICombatant owner, ICombatant healer, ICombatant target, int hpBefore, int healed) { yield break; }
   public virtual IEnumerator OnActionEnd(ICombatant owner, ICombatant actor, bool attacked) { yield break; }
} 
