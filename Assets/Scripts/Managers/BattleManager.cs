using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
public class BattleManager : MonoBehaviour
{
    public static BattleManager instance;
    public static EncounterGroup SelectedEncounter;
    public static string lastTown;
    public enum EncounterType{Normal, Ambush, Advantage}
    public static EncounterType encounterType = EncounterType.Normal;
    List<ActiveStats> players = new List<ActiveStats>();
    List<EnemyAI> enemies = new List<EnemyAI>();
    List<ICombatant> turnOrder = new List<ICombatant>();
    public PartyStatus partyStatusUI;
    bool battleStarted = false;
    bool battleInProgress = false;
    HashSet<ICombatant> reportedDead = new HashSet<ICombatant>();
    bool battleEscaped = false;
    public ActiveStats currentPlayer;
    public float enemySpace = 1.5f;
    public int turnCount = 0;
    public ICombatant firstActorThisTurn;
    bool actionAttacked;
    enum DamageType {Physical, Magic, Fusion}
    public enum AutoMode {Sensible, AttackAllies, AttackEnemies, Chaos}
    bool pendingRedirect;
    public enum DeathHandling {GoToTitleScreen, ReviveInTown}
    public DeathHandling deathHandling = DeathHandling.GoToTitleScreen;
    public string deathSceneName = "DeathScene";
    [Range(0,100)] public int townRevivePenaltyPercent = 50;
    public ElementChart elementChart;
    void Awake()
    {
        if(instance == null) instance = this; 
        else Destroy(gameObject);
    }
    void OnDestroy()
    {
        if(instance == this) instance = null;
    }
    public List<ActiveStats> GetActivePlayers() => players;
    public List<EnemyAI> GetLivingEnemies() => enemies.FindAll(enemy => enemy.currentHP > 0);
    public List<ICombatant> AllCombatants()
    {
        List<ICombatant> all = new List<ICombatant>();
        foreach(ActiveStats player in players) if(player != null) all.Add(player);
        foreach(EnemyAI enemy in enemies) if(enemy != null) all.Add(enemy);
        return all;
    }
IEnumerator ScaledWait(float seconds)
    {
        float speed = SettingsManager.instance != null ? SettingsManager.instance.battleSpeedMultiplier : 1f;
        yield return new WaitForSeconds(seconds / Mathf.Max(0.1f, speed));
    }
void Start()
    {
        if (battleStarted) return;
        battleStarted = true;
        if (SelectedEncounter == null)
        {
            Debug.LogError("Encounter Missing Battle Failed"); return;
        } 
    Debug.Log("Battle started! Loading Encounter");
    turnCount = 0;
    SpawnEnemies();
    FindPlayers();
    UpdateUI();
    if(players.Count > 0 && enemies.Count > 0) StartCoroutine(StartBattle());
    else Debug.LogError("Cannot start battle: Missing Characters"); 
    }
    void FindPlayers()
        {
            players.Clear();
            if(PlayerParty.instance == null) {Debug.LogError("Party is missing"); return;}
            GameObject[] playerObjects = PlayerParty.instance.ActiveParty;
            if (playerObjects.Length == 0){Debug.LogError("All Players Missing"); return;}
            foreach (GameObject playerObject in playerObjects)
            {
                ActiveStats activeStats = playerObject.GetComponent<ActiveStats>();
                if(activeStats != null)
                { 
                    players.Add (activeStats); 
                    activeStats.ResetTransformStat(); 
                    activeStats.ResetStatStages();
                    activeStats.currentMagicAffinity = activeStats.playerStats.magicAffinity;
                    Debug.Log($"Found player: {activeStats.currentName}");}
                else Debug.LogError("Player missing ActiveStats");
            }
        foreach (ActiveStats player in players) player.SetBondPartners(players);
        }
    void SpawnEnemies()
    {
        enemies.Clear();
        if (SelectedEncounter == null) return;
        GameObject spawnAnchor = GameObject.Find(SelectedEncounter.spawnPointName);
        if (spawnAnchor == null) {Debug.LogError("Could not find spawn point"); return;}
        Vector3 basePos = spawnAnchor.transform.position;
        int spawnedCount = 0;
        Debug.Log("SpawningEnemies");
        foreach (GameObject enemyPrefab in SelectedEncounter.enemies)
    {
        if (enemyPrefab == null) continue;
        Vector3 spawnPos = basePos + new Vector3 (spawnedCount * enemySpace, 0, 0);
        GameObject enemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        EnemyAI enemy = enemyObj.GetComponent<EnemyAI>();
        if(enemy != null) 
        {
        enemies.Add(enemy); Debug.Log( $"Spawned: {enemy.currentName}");
         }
        else Debug.LogError("Spawned enemy is missing script");
        spawnedCount++;
         }
   if(enemies.Count == 0) Debug.LogError("No enemies spawned");
    }
    public int GetTurnCount() => turnCount;
    public void UpdateUI()
    {
      if(partyStatusUI != null) partyStatusUI.Refresh(players);
    }
    IEnumerator StartBattle()
        {
            if(battleInProgress) yield break;
            battleInProgress = true;
            reportedDead.Clear();
            BattleHooks.ClearState();
            Debug.Log("Starting Combat");
            if(encounterType == EncounterType.Ambush)
        {
            yield return StartCoroutine(BattleTextBox.instance.ShowMessage("Ambushed. Enemy Strikes The Unprepared Party."));
            yield return StartCoroutine(EnemyOnlyTurn());
        }   
            while (!AllPlayersDead() && !AllEnemiesDead() && !battleEscaped)
            {
                bool grantAdvantage = encounterType == EncounterType.Advantage && turnCount == 0;
                if(grantAdvantage) yield return StartCoroutine(BattleTextBox.instance.ShowMessage("Advantage. The Party Strikes the Unaware Enemy"));
                yield return StartCoroutine(PlayersTurn(grantAdvantage));
            }
            if (battleEscaped)
            {
                yield return
                StartCoroutine(BattleTextBox.instance.ShowMessage("Escape Successful"));
                BattleHooks.EndBattle();
                StopAllCoroutines();
                if(SceneFader.instance != null) SceneFader.instance.LoadScene(RandomEncounter.previousSceneName);
                else SceneManager.LoadScene(RandomEncounter.previousSceneName);
            }
            else yield return StartCoroutine(EndBattle());
        }
        IEnumerator EnemyOnlyTurn()
    {
        List<EnemyAI> livingEnemies = enemies.FindAll(enemy => enemy.currentHP > 0);
        livingEnemies.Sort((first, second) => GetSpeed(second).CompareTo(GetSpeed(first)));
        foreach(EnemyAI enemy in livingEnemies)
        {
            if(enemy.currentHP <= 0) continue;
            yield return StartCoroutine(TurnStartEffects(enemy));
            if(IsDead(enemy)){yield return StartCoroutine(CheckDeath(enemy)); continue;}
            yield return StartCoroutine(EnemyTurn(enemy));
            if(AllPlayersDead()) yield break;
        }
    }
        IEnumerator PlayersTurn(bool skipEnemies = false)
        {
            turnCount++;
            firstActorThisTurn = null;
            Dictionary<ActiveStats, CommandMenu.PlayerActionType> plannedActions = 
            new Dictionary <ActiveStats, CommandMenu.PlayerActionType>();
            Dictionary<ActiveStats, ICombatant> plannedTargets = new Dictionary<ActiveStats, ICombatant>();
            Dictionary<ActiveStats, Learnable> plannedLearnables = new Dictionary<ActiveStats, Learnable>();
            Dictionary<ActiveStats, Item> plannedItems = new Dictionary<ActiveStats, Item>();
            Dictionary<ActiveStats, ActiveStats> plannedAllyTargets = new Dictionary<ActiveStats, ActiveStats>();
            Dictionary<ActiveStats, Spell> plannedSecondSpells = new Dictionary<ActiveStats, Spell>();
            Dictionary<ActiveStats, ICombatant> plannedSecondTargets = new Dictionary<ActiveStats, ICombatant>();
            List<ActiveStats> livingPlayers = players.FindAll(player => player.currentHP > 0); int planIndex = 0;
            while (planIndex < livingPlayers.Count)
            {
                ActiveStats player = livingPlayers[planIndex];
                currentPlayer = player;
                ActivePlayer(player);
                Debug.Log("Choosing Action");
                player.isDefending = false;
                if(BattleHooks.PreventsPlanning(player)) { planIndex++; continue; }
                CommandMenu.instance.NextCharacter(player);
                yield return new WaitUntil(() => CommandMenu.instance.actionSelected || CommandMenu.instance.undoRequested);
                if(CommandMenu.instance.undoRequested)
                {
                    CommandMenu.instance.undoRequested = false;
                    CommandMenu.instance.DisableMenu();
                    if(planIndex > 0) planIndex--;
                    while(planIndex > 0 && BattleHooks.PreventsPlanning(livingPlayers[planIndex])) planIndex--;
                    continue;
                }
                CommandMenu.PlayerActionType chosenAction = CommandMenu.instance.GetSelectedAction();
                Learnable chosenLearnable = CommandMenu.instance.selectedLearnable;
                plannedActions[player] = chosenAction;
                plannedLearnables[player] = chosenLearnable;
                plannedItems[player] = CommandMenu.instance.selectedItem;
                CommandMenu.instance.DisableMenu();
                plannedTargets[player] = CommandMenu.instance.selectedTarget;
                plannedSecondSpells.Remove(player);
                plannedSecondTargets.Remove(player);
                if(chosenAction == CommandMenu.PlayerActionType.Spell && chosenLearnable is Spell firstSpell && BattleHooks.GrantsSecondSpell(player))
            {
                CommandMenu.instance.DualCastActivated(player, firstSpell.Cost);
                yield return new WaitUntil(() => CommandMenu.instance.actionSelected || CommandMenu.instance.undoRequested);
                if(CommandMenu.instance.undoRequested)
                {
                    CommandMenu.instance.undoRequested = false;
                    CommandMenu.instance.DisableMenu();
                    continue;
                }
                CommandMenu.instance.DisableMenu();
                if(!CommandMenu.instance.dualCastSkipped && CommandMenu.instance.selectedLearnable is Spell secondPlanned)
                {
                    plannedSecondSpells[player] = secondPlanned;
                    plannedSecondTargets[player] = CommandMenu.instance.selectedTarget;
                }
            }
                planIndex++;
            }
            CommandMenu.instance.HideMenu();
            BattleTurnOrder();
            List<ICombatant> resolveOrder = skipEnemies ? turnOrder.Where(combatant => combatant is ActiveStats).ToList() :turnOrder;
            foreach (ICombatant combatant in resolveOrder)
            {
                if(IsDead(combatant)) continue;
                if(firstActorThisTurn == null) firstActorThisTurn = combatant;
                yield return StartCoroutine(TurnStartEffects(combatant));
                if(IsDead(combatant)) {yield return StartCoroutine(CheckDeath(combatant)); if(AllPlayersDead() || AllEnemiesDead()) yield break; continue;}
                actionAttacked = false;
                TurnControl control = new TurnControl();
                yield return StartCoroutine(BattleHooks.ControlTurn(combatant, control));
                if(IsDead(combatant)) {yield return StartCoroutine(CheckDeath(combatant)); if(AllPlayersDead() || AllEnemiesDead()) yield break; continue;}
                if ( !control.consumed && combatant is ActiveStats player)
                {
                    ActivePlayer(player);
                    plannedTargets.TryGetValue(player, out ICombatant target);
                    plannedLearnables.TryGetValue(player, out Learnable learnable);
                    plannedItems.TryGetValue(player, out Item item);
                    actionAttacked = false;
                    if(plannedActions.TryGetValue(player, out CommandMenu.PlayerActionType plannedAction))
                    yield return StartCoroutine(PlayerAction(player, plannedAction, target, learnable, item));
                    else yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{player.currentName} can't act this turn."));
                    if(plannedSecondSpells.TryGetValue(player, out Spell secondSpell) && player.currentHP > 0 && !AllEnemiesDead() && !battleEscaped && player.currentMP >= secondSpell.Cost)
                {
                    plannedSecondTargets.TryGetValue(player, out ICombatant secondTarget);
                    yield return StartCoroutine(CastSpell(player, secondSpell, secondTarget));
                    yield return StartCoroutine(ScaledWait(0.5f));
                }
                }
                else if (!control.consumed && combatant is EnemyAI enemy) { actionAttacked = false; yield return StartCoroutine(EnemyTurn(enemy)); }
                yield return StartCoroutine(BattleHooks.OnActionEnd(combatant, actionAttacked));
                if (AllPlayersDead() || AllEnemiesDead() || battleEscaped) yield break;
            }
        } 
        void ActivePlayer(ActiveStats activePlayer)
        {
          if(partyStatusUI != null) partyStatusUI.Refresh(players);
        }
        IEnumerator CheckDeath(ICombatant combatant) 
    {
        if (combatant.currentHP > 0) yield break;
        if(reportedDead.Contains(combatant)) yield break;
        reportedDead.Add(combatant);
        string message = combatant is ActiveStats ? $"{combatant.currentName} has fallen!" : $"{combatant.currentName} was defeated!";
       if(combatant is EnemyAI defeatedEnemy)
        {
           if(BestiaryManager.instance != null) BestiaryManager.instance.Discover(defeatedEnemy.enemyStats);
           if(QuestManager.instance != null) QuestManager.instance.ReportKill(defeatedEnemy.enemyStats);
        }
        yield return StartCoroutine(BattleTextBox.instance.ShowMessage(message)); 
    }
    IEnumerator SkillEffects(ICombatant combatant)
    {
        if(BattleHooks.EffectsSwitchedOff(combatant)) yield break;
        foreach(Skill skill in combatant.equippedSkills)
        {
            if(skill.effects != null)
            foreach(Effect effect in skill.effects)
            if(effect != null) yield return StartCoroutine(effect.Apply(combatant, combatant));
        }
    }
    List<Effect> BuildPhysicalEffects(Equipment weapon, List<Effect> actionEffects)
    {
        List<Effect> combined = new List<Effect>();
        if(actionEffects != null) combined.AddRange(actionEffects);
        if(weapon != null && weapon.effects != null) combined.AddRange(weapon.effects);
        return combined;
    }
    IEnumerator StatusEffectsTimer(ICombatant combatant) 
    {
        List<ActiveStatusEffect> statuses = combatant.activeStatuses;
        if(statuses == null || statuses.Count == 0) yield break;
        List<ActiveStatusEffect> snapshot = new List<ActiveStatusEffect>(statuses);
        foreach(ActiveStatusEffect active in snapshot)
        {
            if(active.statusEffect == null || !statuses.Contains(active)) continue;
            yield return StartCoroutine(active.statusEffect.OnTimer(combatant));
            yield return StartCoroutine(CheckDeath(combatant));
            if(combatant.currentHP <= 0) yield break;
            if(!statuses.Contains(active)) continue;
            active.turnsActive++;
            BattleHooks.RefreshStats(combatant);
            if(active.statusEffect.untilCured) continue;
            active.remainingTurns--;
            if(active.remainingTurns <= 0)
            {
                yield return StartCoroutine(active.statusEffect.OnExpire(combatant));
                BattleHooks.RemoveStatus(combatant, active.statusEffect);
            }
        }
    }
    IEnumerator TurnStartEffects(ICombatant combatant)
    {
        if(combatant is ActiveStats gaugeOwner) gaugeOwner.GaugeTurn();
        yield return StartCoroutine(SkillEffects(combatant));
        yield return StartCoroutine(StatusEffectsTimer(combatant));
    }
    int AccuracyCheck(ICombatant attacker, ICombatant defender, DamageType type, HitContext hit)
    {
        int atk, def;
        int defenderEvasion = Mathf.RoundToInt(defender.Evasion * BattleHooks.EvasionMultiplier(hit));
        switch(type)
        {
            case DamageType.Magic: atk = attacker.Precision; def = defender.Foresight; break;
            case DamageType.Fusion: atk = (attacker.Accuracy + attacker.Precision) / 2; def = (defenderEvasion + defender.Foresight) / 2; break;
            default: atk = attacker.Accuracy; def = defenderEvasion; break;
        }
        return Mathf.Clamp(Mathf.RoundToInt(80 * BattleHooks.BaseHitMultiplier(hit)) + atk - def, 0, 100);
    }
    int CritCheck(ICombatant attacker, ICombatant defender, HitContext hit, out int critStat)
    {
        critStat = attacker.Critical + BattleHooks.CritBonus(hit);
        return Mathf.Clamp(critStat - defender.Dodge, 0, 100);
    }
    HitKind KindOf(DamageType type)
    {
        switch(type)
        {
            case DamageType.Magic: return HitKind.Magic;
            case DamageType.Fusion: return HitKind.Fusion;
            default: return HitKind.Physical;
        }
    }
    Element WeaponElement(Equipment weapon) => weapon != null ? weapon.element : Element.None;
    float GetRaceMultiplier(List<Effect> effects, ICombatant defender)
    {
        if(effects == null || !(defender is EnemyAI targetEnemy) || targetEnemy.enemyStats == null) return 1f;
        foreach(Effect effect in effects)
        {
            if(effect is RaceDamage raceDamage && raceDamage.targetRace != EnemyStats.Race.Unknown
            && targetEnemy.enemyStats.race == raceDamage.targetRace)
            return raceDamage.multiplier;
        }
        return 1f;
    }
    int ComputeDamage(ICombatant attacker, ICombatant defender, DamageType type, int abilityDamage, 
    Element weaponElement, List<Effect> weaponEffects, Element abilityElement, List<Effect> abilityEffects, int strengthBonus = 0, HitContext hit = null)
    {
        int raw; 
        bool ignoreDefense = hit != null && BattleHooks.IgnoresDefenseStats(hit);
        int defenderDefense = ignoreDefense ? 0 : defender.finalDefense;
        int defenderWisdom = ignoreDefense ? 0 : defender.finalWisdom;
        switch(type)
        {
            case DamageType.Magic: raw = attacker.finalMagic - defenderWisdom; break;
            case DamageType.Fusion: raw = (attacker.finalStrength + strengthBonus + attacker.finalMagic) / 2 - (defenderDefense + defenderWisdom) / 2; break;
            default: raw = attacker.finalStrength + strengthBonus - defenderDefense; break;
        }
        float weaponMultiplier = 1f;
        if(type != DamageType.Magic)
        {
            if(weaponElement != Element.None && elementChart != null)
            weaponMultiplier *= BattleHooks.ElementMultiplier(hit, weaponElement, elementChart.GetMultiplier(weaponElement, defender.currentMagicAffinity));
            weaponMultiplier *= GetRaceMultiplier(weaponEffects, defender);
        }
        float abilityMultiplier = 1f;
        if(abilityElement != Element.None && elementChart != null)
        abilityMultiplier *= BattleHooks.ElementMultiplier(hit, abilityElement, elementChart.GetMultiplier(abilityElement, defender.currentMagicAffinity));
        abilityMultiplier *= GetRaceMultiplier(abilityEffects, defender);
        int weaponPortion = Mathf.RoundToInt(raw * weaponMultiplier);
        int abilityPortion = Mathf.RoundToInt(abilityDamage * abilityMultiplier);
        return Mathf.Max(1, weaponPortion + abilityPortion);
    }
    IEnumerator ResolveAttack(ICombatant attacker, ICombatant defender, DamageType type, int abilityDamage, string actionName, Equipment weapon, List<Effect> abilityEffects, Element abilityElement)
    {
        string blockReason = BattleHooks.AttackBlockReason(attacker, KindOf(type), weapon);
        if(blockReason != null)
        {
            yield return StartCoroutine(BattleTextBox.instance.ShowMessage(blockReason));
            yield break;
        }
        Element attackElement = abilityElement != Element.None ? abilityElement : WeaponElement(weapon);
        if(attackElement == Element.Lightning)
        {
            ICombatant rod = BattleHooks.IsLightningRod(attacker) ? attacker : PickLightningRod();
            if(rod != null && rod != defender)
            {
                string text = rod == attacker ? $"{attacker.currentName}'s lightning backfires!" : $"The lightning is drawn to {rod.currentName}!";
                yield return StartCoroutine(BattleTextBox.instance.ShowMessage(text));
                defender = rod;
                pendingRedirect = true;
            }
        }
        yield return StartCoroutine(ResolveHit(attacker, defender, type, abilityDamage, actionName, weapon, abilityEffects, abilityElement));
    }
    ICombatant PickLightningRod()
    {
        List<ICombatant> rods = AllCombatants().FindAll(combatant => combatant.currentHP > 0 && BattleHooks.IsLightningRod(combatant));
        return rods.Count > 0 ? rods[Random.Range(0, rods.Count)] : null;
    }
    IEnumerator ResolveAOE(ICombatant attacker, List<ICombatant> defenders, DamageType type, int abilityDamage, string actionName, Equipment weapon, List<Effect> abilityEffects, Element abilityElement, string targetLabel = "all enemies")
    {
        string blockReason = BattleHooks.AttackBlockReason(attacker, KindOf(type), weapon);
        if(blockReason != null)
        {
            yield return StartCoroutine(BattleTextBox.instance.ShowMessage(blockReason));
            yield break;
        }
        List<ICombatant> targets = defenders;
        bool redirected = false;
        Element attackElement = abilityElement != Element.None ? abilityElement : WeaponElement(weapon);
        if(attackElement == Element.Lightning)
        {
            List<ICombatant> rods = BattleHooks.IsLightningRod(attacker) ? new List<ICombatant> {attacker} : 
            AllCombatants().FindAll(combatant => combatant.currentHP > 0 && BattleHooks.IsLightningRod(combatant));
            if(rods.Count > 0)
            {
                targets = new List<ICombatant>();
                for(int i = 0; i < defenders.Count; i++) targets.Add(rods[i % rods.Count]);
                redirected = true;
                yield return StartCoroutine(BattleTextBox.instance.ShowMessage("The lightning is drawn to them"));
            }
        }
        yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{attacker.currentName} uses {actionName} on {targetLabel}"));
        foreach(ICombatant defender in targets)
        {
            if(defender.currentHP <= 0) continue;
            pendingRedirect = redirected;
             yield return StartCoroutine(ResolveHit(attacker, defender, type, abilityDamage, actionName, weapon, abilityEffects, abilityElement));
        }
    }
    [Range(0f, 0.5f)] public float damageVariance = 0.1f;
    int ApplyVariance(int damage)
    {
        if(damageVariance <= 0f) return damage;
        return Mathf.Max(1, Mathf.RoundToInt(damage * Random.Range(1f - damageVariance, 1f + damageVariance)));
    }
    public bool counterInProgress;
    public IEnumerator CounterAttack(ICombatant counterer, ICombatant target)
    {
        if(counterInProgress || counterer == null || target == null) yield break;
        counterInProgress = true;
        bool savedAttacked = actionAttacked;
        Equipment weapon = counterer is ActiveStats player ? player.BattleWeapon : (counterer as EnemyAI)?.weaponSlot;
        yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{counterer.currentName} counterattacks!"));
        yield return StartCoroutine(ResolveAttack(counterer, target, DamageType.Physical, 0, "counterattacks", weapon, null, Element.None));
        actionAttacked = savedAttacked;
        counterInProgress = false;
    }
    bool lastHitLanded;
    IEnumerator ResolveHit(ICombatant attacker, ICombatant defender, DamageType type, int abilityDamage, string actionName, Equipment weapon, List<Effect> abilityEffects, Element abilityElement)
    {
        actionAttacked = true;
        HitContext hit = new HitContext {attacker = attacker, defender = defender, kind = KindOf(type), abilityElement = abilityElement, weapon = weapon, redirected = pendingRedirect};
        pendingRedirect = false;
        int hits = 1 + BattleHooks.ExtraHits(hit);
        float scale = BattleHooks.HitDamageScale(hit);
        bool anyLanded = false;
        for(int i = 0; i < hits; i++)
        {
            if(i > 0 && defender.currentHP <= 0) break;
            yield return StartCoroutine(ResolveSingleHit(hit, type, abilityDamage, actionName, abilityEffects, scale));
            BattleHooks.HitResolved(hit, lastHitLanded);
            yield return StartCoroutine(BattleHooks.AfterHit(hit, lastHitLanded));
            if(lastHitLanded) anyLanded = true;
        }
        if(!anyLanded) yield break;
        List<Effect> allEffects = BuildPhysicalEffects(BattleHooks.EffectsSwitchedOff(attacker) ? null : weapon, abilityEffects);
        if(allEffects != null) foreach(Effect effect in allEffects) if (effect != null) yield return StartCoroutine(effect.Apply(attacker, defender));
    }
    IEnumerator ResolveSingleHit(HitContext hit, DamageType type, int abilityDamage, string actionName, List<Effect> abilityEffects, float damageScale)
    {
    ICombatant attacker = hit.attacker; ICombatant defender = hit.defender; Equipment weapon = hit.weapon; Element abilityElement = hit.abilityElement;
    lastHitLanded = false;
     hit.damageDealt = 0; hit.blocked = false;
     int hitRate = AccuracyCheck(attacker, defender, type, hit);
     int hitRoll = Random.Range(0, 100);
     bool distractedMiss = hitRoll < hitRate && Random.Range(0f, 100f) < BattleHooks.ExtraMissChance(hit);
     if(hitRoll < hitRate && !distractedMiss)
        {
            Element weaponElement = WeaponElement(weapon);
            List<Effect> weaponEffects = weapon != null  && !BattleHooks.EffectsSwitchedOff(attacker) ? weapon.effects : null;
            lastHitLanded = true;
            int strengthBonus = BattleHooks.StrengthBonus(hit);
            int damage = ComputeDamage(attacker, defender, type, abilityDamage, weaponElement, weaponEffects, abilityElement, abilityEffects, strengthBonus, hit);
            damage += BattleHooks.TrueDamage(hit);
            if(damageScale != 1f) damage = Mathf.Max(1, Mathf.RoundToInt(damage * damageScale));
            float skillMultiplier = BattleHooks.OutgoingMultiplier(hit) * BattleHooks.IncomingMultiplier(hit);
            if(!Mathf.Approximately(skillMultiplier, 1f)) damage = Mathf.Max(1, Mathf.RoundToInt(damage * skillMultiplier));
            damage = ApplyVariance(damage);
            int critRate = CritCheck(attacker, defender, hit, out int critStat);
            bool isCrit = Random.Range(0, 100) < critRate;
            if(isCrit) damage = Mathf.RoundToInt(damage * 3f * BattleHooks.CritDamageMultiplier(hit));
            BlockResult block = BattleHooks.TryBlock(hit);
            if(block != BlockResult.None)
            {
                lastHitLanded = false; hit.blocked = true;
                yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{defender.currentName} blocks {attacker.currentName}'s {actionName}."));
                if(block == BlockResult.Deflected)
                {
                    int reflected = attacker.TakeDamage(damage);
                    UpdateUI();
                    yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"The attack is deflected back. {attacker.currentName} takes {reflected} damage."));
                    yield return StartCoroutine(CheckDeath(attacker));
                }
                yield break;
            }
            bool instantKill = false;
            float killChance = BattleHooks.InstantKillChance(hit);
            if(killChance > 0f && !BattleHooks.IsInstantKillImmune(defender) && Random.Range(0f, 100f) < killChance) instantKill = true;
            int dealt;
            if(instantKill) { dealt = defender.currentHP; defender.currentHP = 0; }
            else dealt = defender.TakeDamage(damage, BattleHooks.IgnoresDefend(hit));
            hit.damageDealt = dealt;
            if(defender is EnemyAI hitEnemy)
            {
                HitFlicker flicker = hitEnemy.GetComponent<HitFlicker>();
                if(flicker != null) flicker.Flicker();
            }
            if(attacker is ActiveStats gainingAttacker) gainingAttacker.GaugeDamageDealt(dealt);
            if(defender is ActiveStats gainingDefender) gainingDefender.GaugeDamageTaken(dealt);
            UpdateUI();
            if(isCrit) yield return StartCoroutine(BattleTextBox.instance.ShowMessage("Critical Hit"));
            yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{attacker.currentName} uses {actionName}. {defender.currentName} takes {dealt} damage."));
            if(instantKill) yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{defender.currentName} never heard it coming!"));
            yield return StartCoroutine(CheckDeath(defender));
            if(defender.currentHP > 0 && dealt > 0) yield return StartCoroutine(BattleHooks.OnDamaged(hit, dealt));
            if(isCrit) yield return StartCoroutine(BattleHooks.OnCrit(hit, critStat));
        }   
        else
        {
            yield return StartCoroutine(BattleTextBox.instance.ShowMessage(distractedMiss ? $"{attacker.currentName} is thrown off by the noise {actionName} missed. " : $"{attacker.currentName}'s {actionName} missed."));
        }
    }
            IEnumerator PlayerAction(ActiveStats player, CommandMenu.PlayerActionType action, 
            ICombatant target, Learnable learnable, Item item)
        {
            switch (action)
            {
                case CommandMenu.PlayerActionType.Attack:
               if (target == null || target.currentHP <= 0) target = GetRandomEnemy();
               if (target != null) yield return StartCoroutine(ResolveAttack(player, target, DamageType.Physical, 0, "attacks", player.BattleWeapon, null, Element.None));
               break;
                    case CommandMenu.PlayerActionType.Defend:
                    player.Defend();
                   yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{player.currentName} defends!"));
                    break;
                    case CommandMenu.PlayerActionType.Transform:
                    player.ActivateTransform();
                    yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{player.currentName} transforms!"));
                    break;
                    case CommandMenu.PlayerActionType.PartySwap:
                {
                    ActiveStats incoming = CommandMenu.instance.selectedSwapTarget;
                    if(incoming == null)
                    {
                        yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{player.currentName} has no swap target selected."));
                        break;
                    }
                    if(incoming.currentHP <= 0 || players.Contains(incoming))
                    {
                        yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{player.currentName} can't swap with {incoming.currentName}."));
                        break;
                    }
                    int swapIndex = players.IndexOf(player);
                    if(swapIndex >= 0)
                    {
                        foreach(ActiveStatusEffect swapStatus in player.activeStatuses) { swapStatus.appliedTurn -= turnCount; swapStatus.source = null; }
                        foreach(ActiveStatusEffect joinStatus in incoming.activeStatuses) joinStatus.appliedTurn += turnCount;
                        players[swapIndex] = incoming;
                        incoming.ResetTransformStat();
                        incoming.ResetStatStages();
                        incoming.currentMagicAffinity = incoming.playerStats.magicAffinity;
                        foreach(ActiveStats plyr in players) plyr.SetBondPartners(players);
                        UpdateUI();
                        yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{player.currentName} swap out for {incoming.currentName}."));
                    }
                    break;
                }
                    case CommandMenu.PlayerActionType.Run:
                   int averageLevel = EnemyLevel();
                    int runChance = 0;
                    int roll = Random.Range(0, 100);
                    runChance += player.finalSpeed;
                   if (player.currentLevel < averageLevel) runChance /= 2;
                   if (roll <= runChance)
                    {
                        battleEscaped = true;
                        yield return StartCoroutine(BattleTextBox.instance.ShowMessage("The Party Escaped"));
                    }
                    else yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{player.currentName} couldn't escape!"));
                    break;
                    case CommandMenu.PlayerActionType.Item:
                    if (item == null)
                {
                    yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{player.currentName} has no item selected!"));
                    break;
                }
                   ICombatant itemTarget = (target != null && target.currentHP > 0) ? target : player;
                   yield return StartCoroutine
                   (BattleTextBox.instance.ShowMessage($"{player.currentName} uses {item.itemName} on {itemTarget.currentName}!"));
                   if(item.effects != null) foreach (Effect effect in item.effects) if(effect != null) yield return StartCoroutine(effect.Apply(player, itemTarget));
                   InventoryManager.instance.LoseItem(item);
                   break;
                    case CommandMenu.PlayerActionType.Art: 
                {
                    Art art = learnable as Art;
                    if(art == null)
                    { 
                        yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{player.currentName} has no art selected"));
                        break; 
                    }
                player.currentHP = Mathf.Max(0, player.currentHP - art.Cost);
                if (art.isAOE)
                    {
                       List<ICombatant> foes = enemies.FindAll(enemy => enemy.currentHP > 0).Cast<ICombatant>().ToList();
                       yield return StartCoroutine(ResolveAOE(player, foes, DamageType.Physical, art.Damage, art.artName, player.BattleWeapon, art.effects, WeaponElement(player.BattleWeapon)));
                    }
                    else
                    {
                        if(target == null || target.currentHP <= 0) target = GetRandomEnemy();
                        if(target != null) yield return StartCoroutine(ResolveAttack(player, target, DamageType.Physical, art.Damage, art.artName, player.BattleWeapon, art.effects, WeaponElement(player.BattleWeapon)));
                    }
                    break;
                    }
                    case CommandMenu.PlayerActionType.Spell:
                   {
                    Spell spell = learnable as Spell;
                    if(spell == null)
                    { 
                        yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{player.currentName} has no spell selected"));
                        break; 
                    }
                    yield return StartCoroutine(CastSpell(player, spell, target));
                    break;
              } 
                    case CommandMenu.PlayerActionType.Fusion:
                   {
                    Fusion fusion = learnable as Fusion;
                    if(fusion == null)
                    { 
                        yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{player.currentName} has no fusion selected"));
                        break; 
                    }
                    player.currentMagicAffinity = fusion.element;
                    player.currentHP = Mathf.Max(0, player.currentHP - fusion.HPCost);
                player.currentMP = Mathf.Max(0, player.currentMP - fusion.MPCost);
                if (fusion.isAOE)
                    {
                       List<ICombatant> foes = enemies.FindAll(enemy => enemy.currentHP > 0).Cast<ICombatant>().ToList();
                       yield return StartCoroutine(ResolveAOE(player, foes, DamageType.Fusion, fusion.Damage, fusion.fusionName, player.BattleWeapon, fusion.effects, fusion.element));
                    }
                    else
                    {
                        if(target == null || target.currentHP <= 0) target = GetRandomEnemy();
                        if(target != null) yield return StartCoroutine(ResolveAttack(player, target, DamageType.Fusion, fusion.Damage, fusion.fusionName, player.BattleWeapon, fusion.effects, fusion.element));
                    }
                    break;
                    }
            }
            if(player.isTransformed) player.TransformTimer();
            UpdateUI();
            yield return StartCoroutine(ScaledWait(0.5f));
        } 
        IEnumerator CastSpell(ActiveStats player, Spell spell, ICombatant target)
    {
        if(BattleHooks.BlocksSpells(player))
        {
            yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{player.currentName} can't breathe, {spell.spellName} fails."));
            yield break;   
        }
        player.currentMagicAffinity = spell.element;
        player.currentMP = Mathf.Max(0, player.currentMP - spell.Cost);
        if(Random.Range(0f, 100f) < BattleHooks.SpellFailChance(player))
        {
            yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{player.currentName} can't hear, making it hard to properly cast spells, thus {spell.spellName} failed"));
            yield break;
        }
        bool misfired = Random.Range(0f, 100f) < BattleHooks.SpellMisfireChance(player);
        if(misfired) yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{player.currentName} is too loud, {spell.spellName} is too strong and explodes, injuring {player.currentName}"));
        bool asSupport = spell.isAOE ? spell.aOETarget == Spell.AOETarget.Allies : target is ActiveStats;
        bool toAllies = asSupport != misfired;
        List<ICombatant> group = toAllies ? players.FindAll(member => member.currentHP > 0).Cast<ICombatant>().ToList() : enemies.FindAll(foe => foe.currentHP > 0).Cast<ICombatant>().ToList();
        if(spell.isAOE)
        {
            if(asSupport)
            {
            yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{player.currentName} uses {spell.spellName} on {(toAllies ? "all allies" : "all enemies")}"));
            foreach(ICombatant member in group) yield return StartCoroutine(ApplySupportEffects(player, member, spell.effects)); 
            }
        else yield return StartCoroutine(ResolveAOE(player, group, DamageType.Magic, spell.Damage, spell.spellName, null, spell.effects, spell.element, toAllies ? "all allies" : "all enemies"));
        yield break;
        }
        ICombatant chosen = target;
        if(misfired) chosen = group.Count > 0 ? group[Random.Range(0, group.Count)] : null;
        else if(asSupport) { if(target.currentHP <= 0) yield break; }
        else if(chosen == null || chosen.currentHP <= 0) chosen = GetRandomEnemy();
        if(chosen == null) yield break;
        if(asSupport)
        {
            yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{player.currentName} uses {spell.spellName} on {chosen.currentName}"));
            yield return StartCoroutine(ApplySupportEffects(player, chosen, spell.effects));
        }
       else yield return StartCoroutine(ResolveAttack(player, chosen, DamageType.Magic, spell.Damage, spell.spellName, null, spell.effects, spell.element));
        }
    IEnumerator ApplySupportEffects(ICombatant caster, ICombatant target, List<Effect> effects)
    {
        if(effects != null) foreach(Effect effect in effects) if(effect != null) yield return StartCoroutine(effect.Apply(caster, target));
        UpdateUI();
    }
    class AutoMove { public Learnable learnable; public bool basic; public bool defend; public bool heal; public bool aoe; }
    List<ICombatant> LivingSide(bool playerSide)
    {
        if(playerSide) return players.FindAll(member => member != null && member.currentHP > 0).Cast<ICombatant>().ToList();
        return enemies.FindAll(foe => foe != null && foe.currentHP > 0).Cast<ICombatant>().ToList();
    }
    public List<ICombatant> AlliesOf(ICombatant who) => LivingSide(who is ActiveStats);
    public List<ICombatant> FoesOf(ICombatant who) => LivingSide(!(who is ActiveStats));
    int MPOf(ICombatant who)
    {
        if(who is ActiveStats player) return player.currentMP;
        if(who is EnemyAI enemy) return enemy.currentMP;
        return 0;
    }
    void SpendMP(ICombatant who, int amount)
    {
        if(who is ActiveStats player) player.currentMP = Mathf.Max(0, player.currentMP - amount);
        else if(who is EnemyAI enemy) enemy.currentMP = Mathf.Max(0, enemy.currentMP - amount);
    }
    Equipment WeaponOfCombatant(ICombatant who)
    {
        if(who is ActiveStats player) return player.BattleWeapon;
        if(who is EnemyAI enemy) return enemy.weaponSlot;
        return null;
    }
    bool AutoCanAfford(ICombatant actor, Learnable learnable)
    {
        int mp = MPOf(actor);
        if(learnable is Art art) return art.canKillUser || actor.currentHP > art.Cost;
        if(learnable is Spell spell) return mp >= spell.Cost;
        if(learnable is Fusion fusion) return (fusion.canKillUser || actor.currentHP > fusion.HPCost) && mp >= fusion.MPCost;
        return false;
        }
        bool IsSupportSpell(Spell spell)
    {
        if(spell.isAOE) return spell.aOETarget == Spell.AOETarget.Allies;
        if(spell.Damage > 0 || spell.effects == null) return false;
        foreach(Effect effect in spell.effects) if(effect is Restore || effect is CureStatusEffect) return true;
        return false;
    }
    List<AutoMove> BuildAutoMoves(ICombatant actor)
    {
        List<AutoMove> moves = new List<AutoMove>();
        moves.Add(new AutoMove{basic = true});
        moves.Add(new AutoMove {defend = true});
        List<Learnable> pool = new List<Learnable>();
        if(actor is ActiveStats player)
        {
            foreach(Art art in player.learnedArts) pool.Add(art);
            foreach(Spell spell in player.learnedSpells) pool.Add(spell);
            foreach(Fusion fusion in player.learnedFusions) pool.Add(fusion);
        }
        else if(actor is EnemyAI enemy)
        {
            foreach(EnemyStats.EnemySpecialAttack special in enemy.currentSpecialAttacks) if(special != null && special.learnable != null) pool.Add(special.learnable);
        }
        foreach(Learnable learnable in pool)
        {
            if(learnable == null || !AutoCanAfford(actor, learnable)) continue;
            AutoMove move = new AutoMove {learnable = learnable};
            if(learnable is Spell spell)
            {
                if(BattleHooks.BlocksSpells(actor)) continue;
                move.heal = IsSupportSpell(spell);
                move.aoe = spell.isAOE;
            }
            else if(learnable is Art art) move.aoe = art.isAOE;
            else if(learnable is Fusion fusion) move.aoe = fusion.isAOE;
            moves.Add(move);
        }
        return moves;
    }
    public IEnumerator AutoAct(ICombatant actor, AutoMode mode, bool includeSelf = false, bool preferAOE = false)
    {
        if(actor == null || actor.currentHP <= 0) yield break;
        List<ICombatant> allies = AlliesOf(actor);
        List<ICombatant> foes = FoesOf(actor);
        List<ICombatant> hurt = allies.FindAll(member => member.finalHP > 0 && member.currentHP * 2 < member.finalHP);
        hurt.Sort((first, second) => first.currentHP.CompareTo(second.currentHP));
        List<AutoMove> allMoves = BuildAutoMoves(actor);
        bool aoeAvailable = allMoves.Exists(move => move.aoe && !move.heal);
        bool noFoes = foes.Count == 0 && mode != AutoMode.AttackAllies && mode != AutoMode.Chaos;
        List<AutoMove> options = new List<AutoMove>();
        List<int> weights = new List<int>();
        foreach(AutoMove move in allMoves)
        {
            int weight = move.basic ? 3 : 2;
            if(move.defend)
            {
                if(mode !=  AutoMode.Sensible && mode != AutoMode.Chaos) continue;
                weight = mode == AutoMode.Sensible && actor.currentHP * 100 < actor.finalHP * 35 ? 4 : 1;
            }
            else if(move.heal)
            {
                if(mode == AutoMode.Sensible) { if(hurt.Count == 0) continue; weight = 4; }
                else if(mode != AutoMode.Chaos) continue;
            }
            else if(noFoes) continue;
            else if(mode == AutoMode.AttackAllies && preferAOE && aoeAvailable && !move.aoe) continue;
            options.Add(move);
            weights.Add(weight);
        }
        int total = 0;
        foreach(int weight in weights) total += weight;
        if(total <= 0) yield break;
        int roll = Random.Range(0, total);
        AutoMove chosen = options[options.Count - 1];
        for(int i = 0; i < options.Count; i++)
        {
            if(roll < weights[i]) { chosen = options[i]; break; }
            roll -= weights[i];
        }
        ICombatant target = null;
        List<ICombatant> group = foes;
        string label = "all enemies";
        if(chosen.heal && mode == AutoMode.Sensible)
        {
            target = hurt[0];
            group = allies;
            label = "all allies";
        }
        else if(mode == AutoMode.AttackAllies)
        {
            List<ICombatant> pool = new List<ICombatant>(allies);
            if(!includeSelf) pool.Remove(actor);
            bool useAllies = pool.Count > 0;
            group = useAllies ? allies : foes;
            label = useAllies ? "all allies" : "all enemies";
            List<ICombatant> singles = useAllies ? pool : foes;
            if(singles.Count > 0) target = singles[Random.Range(0, singles.Count)];
        }
        else if(mode == AutoMode.Chaos)
        {
            List<ICombatant> everyone = new List<ICombatant>(allies);
            everyone.AddRange(foes);
            if(everyone.Count > 0) target = everyone[Random.Range(0, everyone.Count)];
            bool toAllies =  Random.Range(0, 2) == 0;
            group = toAllies ? allies : foes;
            label = toAllies ? "all allies" : "all enemies";
        }
        else if(foes.Count > 0) target = foes[Random.Range(0, foes.Count)];
        if(chosen.defend)
        {
            actor.isDefending = true;
            yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{actor.currentName} defends!"));
        }
    else if(chosen.basic)
        {
           if(target != null) yield return StartCoroutine(ResolveAttack(actor, target, DamageType.Physical, 0, "attacks", WeaponOfCombatant(actor), null, Element.None)); 
        }
        else yield return StartCoroutine(PerformLearnable(actor, chosen.learnable, target, group, label));
        UpdateUI();
        yield return StartCoroutine(ScaledWait(0.5f));
    }
    IEnumerator PerformLearnable(ICombatant actor, Learnable learnable, ICombatant target, List<ICombatant> group, string label)
    {
        Equipment weapon = WeaponOfCombatant(actor);
        if(learnable is Art art)
        {
            actor.currentHP = Mathf.Max(0, actor.currentHP - art.Cost);
            if(actor.currentHP <= 0) { yield return StartCoroutine(CheckDeath(actor)); yield break; }
            if(art.isAOE) yield return StartCoroutine(ResolveAOE(actor, group, DamageType.Physical, art.Damage, art.artName, weapon, art.effects, WeaponElement(weapon), label));
            else if(target != null) yield return StartCoroutine(ResolveAttack(actor, target, DamageType.Physical, art.Damage, art.artName, weapon, art.effects, WeaponElement(weapon)));
        }
        else if(learnable is Fusion fusion)
        {
            actor.currentMagicAffinity = fusion.element;
            actor.currentHP = Mathf.Max(0, actor.currentHP - fusion.HPCost);
            SpendMP(actor, fusion.MPCost);
            if(actor.currentHP <= 0) { yield return StartCoroutine(CheckDeath(actor)); yield break; }
            if(fusion.isAOE) yield return StartCoroutine(ResolveAOE(actor, group, DamageType.Fusion, fusion.Damage, fusion.fusionName, weapon, fusion.effects, fusion.element, label));
            else if(target != null) yield return StartCoroutine(ResolveAttack(actor, target, DamageType.Fusion, fusion.Damage, fusion.fusionName, weapon, fusion.effects, fusion.element));
        }
        else if(learnable is Spell spell)
        {
            actor.currentMagicAffinity = spell.element;
            SpendMP(actor, spell.Cost);
            if(IsSupportSpell(spell))
            {
                if(spell.isAOE)
                {
                    yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{actor.currentName} uses {spell.spellName} on {label}"));
                    foreach(ICombatant member in group) yield return StartCoroutine(ApplySupportEffects(actor, member, spell.effects));
                }
                else if(target != null)
                {
                  yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{actor.currentName} uses {spell.spellName} on {target.currentName}"));
                  yield return StartCoroutine(ApplySupportEffects(actor, target, spell.effects));
                }
            }
        else if(spell.isAOE) yield return StartCoroutine(ResolveAOE(actor, group, DamageType.Magic, spell.Damage, spell.spellName, null, spell.effects, spell.element, label));
        else if(target != null) yield return StartCoroutine(ResolveAttack(actor, target, DamageType.Magic, spell.Damage, spell.spellName, null, spell.effects, spell.element));
}
    }
        IEnumerator EnemyUseItem(EnemyAI enemy)
    {
        List<Item> consumables = enemy.personalInventory.items.OfType<Item>()
        .Where(item => item.itemType == Item.ItemType.Consumable).ToList();
        if(consumables.Count == 0)
        {
            List<ActiveStats> livingPlayers = players.FindAll(player => player.currentHP > 0);
            ActiveStats fallbackTarget = enemy.ChooseTarget(livingPlayers);
            if(fallbackTarget != null)
            yield return StartCoroutine(ResolveAttack(enemy, fallbackTarget, DamageType.Physical, 0, "attacks", enemy.weaponSlot, null, Element.None));
            yield break;
        }
        Item chosen = consumables[Random.Range(0, consumables.Count)];
        yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{enemy.currentName} uses {chosen.itemName}!"));
        if(chosen.effects != null)
        foreach(Effect effect in chosen.effects)
        if(effect != null) yield return StartCoroutine(effect.Apply(enemy, enemy));
        enemy.personalInventory.RemoveItem(chosen);
    }
         IEnumerator EnemyTurn(EnemyAI enemy)
        {
            enemy.isDefending = false;
            enemy.ChangeAI();
            enemy.CheckTransform();
            List<ActiveStats> livingPlayers = players.FindAll(player => player.currentHP > 0);
            EnemyAI.EnemyActionType action = enemy.ChooseAction();
            switch (action)
        {
            case EnemyAI.EnemyActionType.Attack:
            ActiveStats target = enemy.ChooseTarget(livingPlayers);
            if(target != null)
                {
                    EnemyStats.EnemySpecialAttack move = enemy.ChooseAttackMove();
                    if (move == null) yield return
                    StartCoroutine(ResolveAttack(enemy, target, DamageType.Physical, 0, "attacks", enemy.weaponSlot, null, Element.None));
                    else yield return StartCoroutine(EnemySpecial(enemy, target, move));
                }
                break;
                case EnemyAI.EnemyActionType.Defend:
                enemy.Defend();
                yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{enemy.currentName} defends!"));
                break;
                case EnemyAI.EnemyActionType.Item:
                yield return StartCoroutine(EnemyUseItem(enemy));
                break;
                case EnemyAI.EnemyActionType.Run:
                yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{enemy.currentName} Flees!"));
                break;
        }
        yield return StartCoroutine(ScaledWait(1f));
        } 
    IEnumerator EnemySpecial(EnemyAI enemy, ActiveStats target, EnemyStats.EnemySpecialAttack special)
    {
        Learnable learnable = special.learnable;
        if(learnable is Spell && BattleHooks.BlocksSpells(enemy))
        {
            yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{enemy.currentName} can't breathe, spell's cannot be used"));
            yield break;
        }
        enemy.PayCost(learnable);
        bool misfired = false;
         if(learnable is Spell spell)
        {
            if(Random.Range(0f, 100f) < BattleHooks.SpellFailChance(enemy))
            {
                yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{enemy.currentName} can't hear. The spell fails"));
                yield break;
            }
           misfired = Random.Range(0f, 100f) < BattleHooks.SpellMisfireChance(enemy);
           if(misfired) yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{enemy.currentName} is too loud. The spell explodes damaging the {enemy.currentName}"));
        }
        List<ICombatant> livingPlayers = players.FindAll(player => player.currentHP > 0).Cast<ICombatant>().ToList();
        if(learnable is Art art)
        {
            if(art.isAOE) yield return StartCoroutine(ResolveAOE(enemy, livingPlayers, DamageType.Physical, art.Damage, art.artName, enemy.weaponSlot, art.effects, WeaponElement(enemy.weaponSlot)));
            else yield return StartCoroutine(ResolveAttack(enemy, target, DamageType.Physical, art.Damage, art.artName, enemy.weaponSlot, art.effects, WeaponElement(enemy.weaponSlot)));
        }
        if(learnable is Spell castSpell)
        {
            enemy.currentMagicAffinity = castSpell.element;
            List<ICombatant> spellGroup = misfired ? enemies.FindAll(foe => foe.currentHP > 0).Cast<ICombatant>().ToList() : livingPlayers;
            if(castSpell.isAOE) yield return StartCoroutine(ResolveAOE(enemy, spellGroup, DamageType.Magic, castSpell.Damage, castSpell.spellName, null, castSpell.effects, castSpell.element, misfired ? "its own allies" : "all enemies"));
            else
            {
                ICombatant spellTarget = misfired && spellGroup.Count > 0 ? spellGroup[Random.Range(0, spellGroup.Count)] : target;
                yield return StartCoroutine(ResolveAttack(enemy, spellTarget, DamageType.Magic, castSpell.Damage, castSpell.spellName, null, castSpell.effects, castSpell.element));
            }
        }
        if(learnable is Fusion fusion)
        {
            enemy.currentMagicAffinity = fusion.element;
            if(fusion.isAOE) yield return StartCoroutine(ResolveAOE(enemy, livingPlayers, DamageType.Fusion, fusion.Damage, fusion.fusionName, enemy.weaponSlot, fusion.effects, fusion.element));
            else yield return StartCoroutine(ResolveAttack(enemy, target, DamageType.Fusion, fusion.Damage, fusion.fusionName, enemy.weaponSlot, fusion.effects, fusion.element));
        }
    }
        void BattleTurnOrder()
        {
            turnOrder.Clear();
            players.RemoveAll(player => player == null);
            enemies.RemoveAll(enemy => enemy == null);
            turnOrder.AddRange(players.FindAll(player => player.currentHP > 0).Cast<ICombatant>());
            turnOrder.AddRange(enemies.FindAll(enemy => enemy.currentHP > 0).Cast<ICombatant>());
            turnOrder.Sort((first, second) => GetSpeed(second).CompareTo(GetSpeed(first)));
        }
        int EnemyLevel()
        {
        int totalLevel = 0;
        foreach(EnemyAI enemy in enemies)
        {
            totalLevel += enemy.currentLevel;
        }
        return totalLevel / enemies.Count;
        }
        int GetSpeed(ICombatant combatant)
        {
            return combatant.finalSpeed + Random.Range(-5, 5);
        }
        bool IsDead(ICombatant combatant) => combatant.currentHP <= 0;
        EnemyAI GetRandomEnemy()
        {
            List <EnemyAI> aliveEnemies = enemies.FindAll(enemy => enemy.currentHP > 0);
            if (aliveEnemies.Count > 0) return aliveEnemies[Random.Range(0, aliveEnemies.Count)]; else return null;
        }
        bool AllPlayersDead() => players.TrueForAll(player => player.currentHP <= 0);
        bool AllEnemiesDead() => enemies.TrueForAll(enemy => enemy.currentHP <= 0);
        IEnumerator EndBattle()
        {
            BattleHooks.EndBattle();
            if (AllPlayersDead())
            {
                yield return
                StartCoroutine(BattleTextBox.instance.ShowMessage("Party Defeated"));
                yield return StartCoroutine(ScaledWait(1.5f));
                StopAllCoroutines();
                PartyDefeat();
            }
            else if(AllEnemiesDead())
            {
                yield return
                StartCoroutine(BattleTextBox.instance.ShowMessage("All Enemies Dead"));
               yield return StartCoroutine(BattleRewards());
               yield return StartCoroutine(ScaledWait(1.5f));
                StopAllCoroutines();
                if(SceneFader.instance != null) SceneFader.instance.LoadScene(RandomEncounter.previousSceneName);
                else SceneManager.LoadScene(RandomEncounter.previousSceneName);
            }
        }
    void PartyDefeat()
    {
        if(FlagManager.instance != null) FlagManager.instance.SetFlag("PartyDefeated", true);
        switch(deathHandling)
        {
            case DeathHandling.ReviveInTown:
            ReviveInTown();
            break;
            default:
            DeathScreen();
            break;
        }
    }
    void DeathScreen()
    {
        if(SceneFader.instance != null) SceneFader.instance.LoadScene(deathSceneName);
        else SceneManager.LoadScene(deathSceneName);
    }
    void ReviveInTown()
    {
        if(WalletManager.instance != null)
        {
            int penalty = WalletManager.instance.currentGold * townRevivePenaltyPercent / 100;
        WalletManager.instance.SpendGold(penalty);
        }
        foreach(ActiveStats player in players)
        {
            player.Heal(player.finalHP);
            player.RestoreMP(player.finalMP);
            player.ClearStatus();
        }
      if(string.IsNullOrEmpty(lastTown))
        {
            Debug.LogWarning("No town visited.");
            DeathScreen();
        }
        else if(SceneFader.instance != null) SceneFader.instance.LoadScene(lastTown);
        else SceneManager.LoadScene(lastTown);
    }
    IEnumerator BattleRewards()
    {
        int totalExp = 0;
        int totalGold = 0;
        List<EnemyStats.LootDrop> earnedDrops = new List<EnemyStats.LootDrop>();
        foreach (EnemyAI enemy in enemies)
        {
            if(enemy == null) continue;
            totalExp += Random.Range(enemy.enemyStats.minExpReward, enemy.enemyStats.maxExpReward + 1);    
            totalGold += Random.Range(enemy.enemyStats.minGoldReward, enemy.enemyStats.maxGoldReward + 1);
            foreach (EnemyStats.LootDrop drop in enemy.enemyStats.lootTable)
            {
                if (drop.item == null) continue;
                int roll = Random.Range(0, 100);
                if(roll < drop.dropChance) earnedDrops.Add(drop);
            }
        }
        List<ActiveStats> livingPlayers = players.FindAll(player => player.currentHP > 0);
        if(totalGold > 0) 
        {
        WalletManager.instance.AddGold(totalGold);
        yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"The party earns {totalGold} gold!"));
        }
        if(totalExp > 0 && livingPlayers.Count > 0)
        yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"The party gains {totalExp} experience!"));
        foreach(ActiveStats player in livingPlayers)
        yield return StartCoroutine(player.GainExperienceRoutine(totalExp));
        for(int i = 0; i < livingPlayers.Count; i++)
        {
                for (int j = i + 1; j < livingPlayers.Count; j++)
                {
                    livingPlayers[i].AddBondPoints(livingPlayers[j].playerStats);
                    livingPlayers[j].AddBondPoints(livingPlayers[i].playerStats);
                }
            }
        foreach (EnemyStats.LootDrop drop in earnedDrops)
        {
            for (int q = 0; q < drop.quantity; q++)
            InventoryManager.instance.PickupItem(drop.item);
            yield return StartCoroutine(BattleTextBox.instance.ShowMessage($" Found {drop.item.itemName} x{drop.quantity}"));
        }
    }
}