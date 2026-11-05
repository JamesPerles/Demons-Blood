using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum HitKind { Physical, Magic, Fusion }
public enum BlockResult { None, Blocked, Deflected }
public class HitContext
{
    public ICombatant attacker;
    public ICombatant defender;
    public HitKind kind;
    public Element abilityElement;
    public Equipment weapon;
    public int damageDealt;
    public bool blocked;
    public bool redirected;
    public Element WeaponElement => weapon != null ? weapon.element : Element.None;
    public Element AttackElement => abilityElement != Element.None ? abilityElement : WeaponElement;
}
public class TurnControl {public bool consumed; }
public static class BattleHooks
{
    struct HookEntry { public ICombatant owner; public BattleHookEffect hook; }
    static readonly Dictionary<ICombatant, Dictionary<object, object>> state = new Dictionary<ICombatant, Dictionary<object, object>>();
    public static void ClearState() => state.Clear();
    public static T Get<T>(ICombatant owner, object key)
    {
        if(owner == null || key == null) return default(T);
        if(state.TryGetValue(owner, out Dictionary<object, object> values) && 
        values.TryGetValue(key, out object value) && value is T typed) return typed;
        return default(T);
    }
        public static void Set(ICombatant owner, object key, object value)
    {
        if(owner == null || key == null) return;
        if(!state.TryGetValue(owner, out Dictionary<object, object> values)) 
        { values = new Dictionary<object, object>(); state[owner] = values; }
        if(value == null) values.Remove(key); else values[key] = value;
    }
    public static int GetInt(ICombatant owner, object key) => Get<int>(owner, key);
    public static void SetInt(ICombatant owner, object key, int value) => Set(owner, key, value == 0 ? null : (object)value);
    public static void SetMomentum(ICombatant combatant, object source, float percent)
    {
        if(combatant is ActiveStats player) player.SetMomentum(source, percent);
        else if(combatant is EnemyAI enemy) enemy.SetMomentum(source, percent);
    }
    public static void EndBattle()
    {
        HashSet<ICombatant> owners = new HashSet<ICombatant>(state.Keys);
        if(BattleManager.instance != null) foreach(ICombatant combatant in 
        BattleManager.instance.AllCombatants()) owners.Add(combatant);
        foreach(ICombatant owner in owners)
        foreach(BattleHookEffect hook in Of(owner)) hook.OnBattleEnd(owner);
        if(BattleManager.instance != null)
        {
            int turns = BattleManager.instance.turnCount;
            foreach(ICombatant combatant in BattleManager.instance.AllCombatants())
            {
                if(combatant.activeStatuses == null) continue;
                foreach(ActiveStatusEffect active in combatant.activeStatuses)
                {
                    active.appliedTurn -= turns;
                    active.source = null;
                }
            }
            BattleManager.instance.turnCount = 0;
        }
        state.Clear();
    }
    public static int TurnNumber => BattleManager.instance != null ? BattleManager.instance.turnCount : 0;
    public static ICombatant FirstActor => BattleManager.instance != null ? 
    BattleManager.instance.firstActorThisTurn : null;
    public static List<ActiveStats> PartyOf(ICombatant owner)
    {
        if(BattleManager.instance != null && owner is ActiveStats) return BattleManager.instance.GetActivePlayers();
        return new List<ActiveStats>();
    }
    public static bool IsAfflicted(ICombatant combatant)
    {
        if(combatant == null) return false;
        if(combatant.activeStatuses != null && combatant.activeStatuses.Count > 0) return true;
        if(combatant is ActiveStats player) return player.HasStatDebuff;
        if(combatant is EnemyAI enemy) return enemy.HasStatDebuff;
        return false;
    }
    public static List<BattleHookEffect> Of(ICombatant combatant)
    {
        List<BattleHookEffect> hooks = new List<BattleHookEffect>();
        if(combatant == null) return hooks;
        List<BattleHookEffect> statusHooks = new List<BattleHookEffect>();
        if(combatant.activeStatuses != null)
        foreach(ActiveStatusEffect active in combatant.activeStatuses)
        if(active != null && active.statusEffect != null) statusHooks.Add(active.statusEffect);
        foreach(BattleHookEffect hook in statusHooks)
        if(hook.SwitchesOffEffects(combatant)) return statusHooks;
        if(combatant is ActiveStats player) AddFromEquipment(hooks, 
        player.weaponSlot, player.headSlot, player.bodySlot, player.shieldSlot, player.accessorySlot);
        else if(combatant is EnemyAI enemy) AddFromEquipment
        (hooks, enemy.weaponSlot, enemy.headSlot, enemy.bodySlot, enemy.shieldSlot, enemy.accessorySlot);
        hooks.AddRange(statusHooks);
        List<Skill> skills = combatant.equippedSkills;
        if(skills != null)
        foreach(Skill skill in skills)
            {
                if(skill == null || skill.effects == null) continue;
                foreach(Effect effect in skill.effects) if(effect is BattleHookEffect hook) hooks.Add(hook);
            }
            return hooks;
     }
     static void AddFromEquipment(List<BattleHookEffect> hooks, params Equipment[] gear)
    {
        foreach(Equipment equipment in gear)
        {
            if(equipment == null || equipment.effects == null) continue;
            foreach(Effect effect in equipment.effects) if(effect is BattleHookEffect hook) hooks.Add(hook);
        }
    }
    public static bool EffectsSwitchedOff(ICombatant owner)
    {
        if(owner == null || owner.activeStatuses == null) return false;
        foreach(ActiveStatusEffect active in owner.activeStatuses)
        if(active != null && active.statusEffect != null && active.statusEffect.SwitchesOffEffects(owner)) return true;
        return false;
    }
    public static bool BlocksStatChanges(ICombatant owner)
    {
        if(owner == null || owner.activeStatuses == null) return false;
        foreach(ActiveStatusEffect active in owner.activeStatuses)
        if(active != null && active.statusEffect != null && active.statusEffect.BlocksStatChanges(owner)) return true;
        return false;
    }
    static List<HookEntry> Everyone()
    {
        List<HookEntry> entries = new List<HookEntry>();
        if(BattleManager.instance == null) return entries;
        foreach(ICombatant combatant in BattleManager.instance.AllCombatants())
        foreach(BattleHookEffect hook in Of(combatant)) entries.Add(new HookEntry {owner = combatant, hook = hook});
        return entries;
    }
    public static float OutgoingMultiplier(HitContext hit)
    {
        float multiplier = 1f;
        foreach(HookEntry entry in Everyone()) multiplier *= entry.hook.OutgoingDamageMultiplier(entry.owner, hit);
        return multiplier;
    }
    public static float IncomingMultiplier(HitContext hit)
    {
        float multiplier = 1f;
        foreach(HookEntry entry in Everyone()) multiplier *= entry.hook.IncomingDamageMultiplier(entry.owner, hit);
        return multiplier;
    }
    public static int CritBonus(HitContext hit)
    {
        int bonus = 0;
        foreach(HookEntry entry in Everyone()) bonus += entry.hook.CritBonus(entry.owner, hit);
        return bonus;
    }
    public static float EvasionMultiplier(HitContext hit)
    {
        float multiplier = 1f;
        foreach(HookEntry entry in Everyone()) multiplier *= entry.hook.EvasionMultiplier(entry.owner, hit);
        return multiplier;
    }
    public static float ElementMultiplier(HitContext hit, Element element, float multiplier)
    {
        if(hit == null) return multiplier;
        foreach(HookEntry entry in Everyone()) multiplier = entry.hook.ModifyElementMultiplier(entry.owner, hit, element, multiplier);
        return multiplier;
    }
     public static int StatusChanceBonus(ICombatant caster, ICombatant target, StatusEffect status)
    {
        int bonus = 0;
        foreach(HookEntry entry in Everyone()) bonus += entry.hook.StatusChanceBonus(entry.owner, caster, target, status);
        return bonus;
    }
     public static int LimitDamage(ICombatant owner, int damage)
    {
        foreach(BattleHookEffect hook in Of(owner)) damage = hook.LimitDamage(owner, damage);
        return Mathf.Max(1, damage);
    }
     public static void FlatStats(ICombatant owner, StatCalculatorResult result)
    {
        foreach(BattleHookEffect hook in Of(owner)) hook.ModifyFlatStats(owner, result);
    }
    public static float StatMultiplier(ICombatant owner, Stat stat)
    {
        float multiplier = 1f;
        foreach(BattleHookEffect hook in Of(owner)) multiplier *= hook.StatMultiplier(owner, stat);
        return multiplier;
    }
    public static int ExtraHits(HitContext hit)
    {
        int extra = 0;
        foreach(HookEntry entry in Everyone()) extra += entry.hook.ExtraHits(entry.owner, hit);
        return extra;
    }
    public static float HitDamageScale(HitContext hit)
    {
        float scale = 1f;
        foreach(HookEntry entry in Everyone()) scale *= entry.hook.HitDamageScale(entry.owner, hit);
        return scale;
    }
public static int StrengthBonus(HitContext hit)
    {
        int bonus = hit.weapon != null && !GearStatsDisabled(hit.attacker) ? hit.weapon.WeaponStrengthBonus : 0;
        foreach(HookEntry entry in Everyone()) bonus += entry.hook.StrengthBonus(entry.owner, hit);
        return bonus;
    }
    public static void HitResolved(HitContext hit, bool landed)
    {
        foreach(HookEntry entry in Everyone()) entry.hook.OnHitResolved(entry.owner, hit, landed);
    }
     public static bool GrantsSecondSpell(ICombatant owner)
    {
        foreach(BattleHookEffect hook in Of(owner)) if(hook.GrantsSecondSpell(owner)) return true;
        return false;
    }
    public static IEnumerator OnCrit(HitContext hit, int critStat)
    {
        foreach(HookEntry entry in Everyone()) yield return entry.hook.OnCrit(entry.owner, hit, critStat);
    }
    public static IEnumerator OnHeal(ICombatant healer, ICombatant target, int hpBefore, int healed)
    {
        foreach(HookEntry entry in Everyone()) yield return entry.hook.OnHeal(entry.owner, healer, target, hpBefore, healed);
    }
    public static bool IgnoresDefend(HitContext hit)
    {
         foreach(HookEntry entry in Everyone()) if(entry.hook.IgnoresDefend(entry.owner, hit)) return true;
         return false;
    }
    public static int TrueDamage(HitContext hit)
    {
        int total = 0;
         foreach(HookEntry entry in Everyone()) total += entry.hook.TrueDamage(entry.owner, hit);
        return total;
    }
    public static BlockResult TryBlock(HitContext hit)
    {
        foreach(HookEntry entry in Everyone())
        {
            if(ReactionsLocked(entry.owner)) continue;
            BlockResult result = entry.hook.TryBlock(entry.owner, hit);
            if(result == BlockResult.None) continue;
            float keep = BlockChanceMultiplier(hit);
            if(keep < 1f && Random.value >= keep) continue;
            return result;
        }
        return BlockResult.None;
    }
     public static IEnumerator AfterHit(HitContext hit, bool landed)
    {
        foreach(HookEntry entry in Everyone())
        {
        if(ReactionsLocked(entry.owner)) continue;
        yield return entry.hook.AfterHit(entry.owner, hit, landed);
        }
    }
    public static IEnumerator OnActionEnd(ICombatant actor, bool attacked)
    {
        foreach(HookEntry entry in Everyone()) yield return entry.hook.OnActionEnd(entry.owner, actor, attacked);
    }
    public static readonly object DistractedKey = new object();
    public static bool TryApplyStatus(ICombatant who, StatusEffect status, ICombatant source = null)
    {
        if(who is ActiveStats player) return player.ApplyStatus(status, source);
        if(who is EnemyAI enemy) return enemy.ApplyStatus(status, source);
        return false;
    }
    public static void RemoveStatus(ICombatant who, StatusEffect status)
    {
        if(who is ActiveStats player) player.RemoveStatus(status);
        else if(who is EnemyAI enemy) enemy.RemoveStatus(status);
    }
    public static void RefreshStats(ICombatant who)
    {
        if(who is ActiveStats player) player.RefreshStats();
        else if(who is EnemyAI enemy) enemy.RefreshStats();
    }
    public static bool IsInstantKillImmune(ICombatant who) => who is EnemyAI enemy && enemy.immuneToInstantKill;
    public static bool GearStatsDisabled(ICombatant owner)
    {
        foreach(BattleHookEffect hook in Of(owner)) if(hook.GearStatsDisabled(owner)) return true;
        return false;
    }
    public static float BaseHitMultiplier(HitContext hit)
    {
        float multiplier = 1f;
        foreach(BattleHookEffect hook in Of(hit.attacker)) multiplier *= hook.BaseHitMultiplier(hit.attacker, hit);
        return multiplier;
    }
    public static bool IgnoresDefenseStats(HitContext hit)
    {
        foreach(HookEntry entry in Everyone()) if(entry.hook.IgnoresDefenseStats(entry.owner, hit)) return true;
        return false;
    }
    public static float CritDamageMultiplier(HitContext hit)
    {
        float multiplier = 1f;
        foreach(HookEntry entry in Everyone()) multiplier *= entry.hook.CritDamageMultiplier(entry.owner, hit);
        return multiplier;
    }
    public static float BlockChanceMultiplier(HitContext hit)
    {
        float multiplier = 1f;
        foreach(HookEntry entry in Everyone()) multiplier *= entry.hook.BlockChanceMultiplier(entry.owner, hit);
        return multiplier;
    }
    public static float ExtraMissChance(HitContext hit)
    {
        float survive = 1f;
        foreach(HookEntry entry in Everyone()) survive *= 1f - Mathf.Clamp01(entry.hook.ExtraMissChance(entry.owner, hit) / 100f);
        return (1f - survive) * 100f;
    }
    public static float InstantKillChance(HitContext hit)
    {
        float survive = 1f;
        foreach(HookEntry entry in Everyone()) survive *= 1f - Mathf.Clamp01(entry.hook.InstantKillChance(entry.owner, hit) / 100f);
        return (1f - survive) * 100f;
    }
    static bool ReactionsLocked(ICombatant owner)
    {
        foreach(BattleHookEffect hook in Of(owner)) if(hook.DisablesReactions(owner)) return true;
        return false;
    }
    public static string AttackBlockReason(ICombatant attacker, HitKind kind, Equipment weapon)
    {
        foreach(BattleHookEffect hook in Of(attacker))
        {
            string reason = hook.BlockAttackReason(attacker, kind, weapon);
            if(reason != null) return reason;
        }
        if(TurnNumber > 0 && GetInt(attacker, DistractedKey) == TurnNumber) return $"{attacker.currentName} is too distracted to attack.";
        return null;
    }
    public static bool BlocksSpells(ICombatant caster)
    {
        foreach(BattleHookEffect hook in Of(caster)) if(hook.BlocksSpells(caster)) return true;
        return false;
    }
    public static float SpellFailChance(ICombatant caster)
    {
        float survive = 1f;
        foreach(BattleHookEffect hook in Of(caster)) survive *= 1f - Mathf.Clamp01(hook.SpellFailChance(caster) / 100f);
        return (1f - survive) * 100f;
    }
    public static float SpellMisfireChance(ICombatant caster)
    {
        float survive = 1f;
        foreach(BattleHookEffect hook in Of(caster)) survive *= 1f - Mathf.Clamp01(hook.SpellMisfireChance(caster) / 100f);
        return (1f - survive) * 100f;
    }
    public static bool PreventsPlanning(ICombatant who)
    {
        foreach(BattleHookEffect hook in Of(who)) if(hook.PreventsPlanning(who)) return true;
        return false;
    }
    public static bool IsLightningRod(ICombatant who)
    {
        foreach(BattleHookEffect hook in Of(who)) if(hook.AttractsLightning(who)) return true;
        return false;
    }
    public static IEnumerator ControlTurn(ICombatant who, TurnControl control)
    {
        foreach(BattleHookEffect hook in Of(who))
        {
            yield return hook.ControlTurn(who, control);
            if(control.consumed) yield break;
        }
    }
    public static IEnumerator OnDamaged(HitContext hit, int dealt)
    {
        foreach(BattleHookEffect hook in Of(hit.defender)) yield return hook.OnDamaged(hit.defender, hit, dealt);
    }
    }