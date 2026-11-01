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
    public Element WeaponElement => weapon != null ? weapon.element : Element.None;
    public Element AttackElement => abilityElement != Element.None ? abilityElement : WeaponElement;
}
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
        if(combatant is ActiveStats player) AddFromEquipment(hooks, 
        player.weaponSlot, player.headSlot, player.bodySlot, player.shieldSlot, player.accessorySlot);
        else if(combatant is EnemyAI enemy) AddFromEquipment
        (hooks, enemy.weaponSlot, enemy.headSlot, enemy.bodySlot, enemy.shieldSlot, enemy.accessorySlot);
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
        int bonus = hit.weapon != null ? hit.weapon.WeaponStrengthBonus : 0;
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
            BlockResult result = entry.hook.TryBlock(entry.owner, hit);
            if(result != BlockResult.None) return result;
        }
        return BlockResult.None;
    }
     public static IEnumerator AfterHit(HitContext hit, bool landed)
    {
        foreach(HookEntry entry in Everyone()) yield return entry.hook.AfterHit(entry.owner, hit, landed);
    }
    public static IEnumerator OnActionEnd(ICombatant actor, bool attacked)
    {
        foreach(HookEntry entry in Everyone()) yield return entry.hook.OnActionEnd(entry.owner, actor, attacked);
    }
    }