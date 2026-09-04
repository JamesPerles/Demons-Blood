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
    enum DamageType {Physical, Magic, Fusion}
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
                StopAllCoroutines();
                SceneManager.LoadScene(RandomEncounter.previousSceneName);
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
            Dictionary<ActiveStats, CommandMenu.PlayerActionType> plannedActions = 
            new Dictionary <ActiveStats, CommandMenu.PlayerActionType>();
            Dictionary<ActiveStats, ICombatant> plannedTargets = new Dictionary<ActiveStats, ICombatant>();
            Dictionary<ActiveStats, Learnable> plannedLearnables = new Dictionary<ActiveStats, Learnable>();
            Dictionary<ActiveStats, Item> plannedItems = new Dictionary<ActiveStats, Item>();
            Dictionary<ActiveStats, ActiveStats> plannedAllyTargets = new Dictionary<ActiveStats, ActiveStats>();
            List<ActiveStats> livingPlayers = players.FindAll(player => player.currentHP > 0); int planIndex = 0;
            while (planIndex < livingPlayers.Count)
            {
                ActiveStats player = livingPlayers[planIndex];
                currentPlayer = player;
                ActivePlayer(player);
                Debug.Log("Choosing Action");
                player.isDefending = false;
                CommandMenu.instance.NextCharacter(player);
                yield return new WaitUntil(() => CommandMenu.instance.actionSelected || CommandMenu.instance.undoRequested);
                if(CommandMenu.instance.undoRequested)
                {
                    CommandMenu.instance.undoRequested = false;
                    CommandMenu.instance.DisableMenu();
                    if(planIndex > 0) planIndex--;
                    continue;
                }
                CommandMenu.PlayerActionType chosenAction = CommandMenu.instance.GetSelectedAction();
                Learnable chosenLearnable = CommandMenu.instance.selectedLearnable;
                plannedActions[player] = chosenAction;
                plannedLearnables[player] = chosenLearnable;
                plannedItems[player] = CommandMenu.instance.selectedItem;
                CommandMenu.instance.DisableMenu();
                plannedTargets[player] = CommandMenu.instance.selectedTarget;
                planIndex++;
            }
            CommandMenu.instance.HideMenu();
            BattleTurnOrder();
            List<ICombatant> resolveOrder = skipEnemies ? turnOrder.Where(combatant => combatant is ActiveStats).ToList() :turnOrder;
            foreach (ICombatant combatant in resolveOrder)
            {
                if(IsDead(combatant)) continue;
                yield return StartCoroutine(TurnStartEffects(combatant));
                if(IsDead(combatant)) {yield return StartCoroutine(CheckDeath(combatant)); continue;}
                if ( combatant is ActiveStats player)
                {
                    ActivePlayer(player);
                    plannedTargets.TryGetValue(player, out ICombatant target);
                    plannedLearnables.TryGetValue(player, out Learnable learnable);
                    plannedItems.TryGetValue(player, out Item item);
                    yield return StartCoroutine(PlayerAction(player, plannedActions[player], target, learnable, item));
                }
                else if (combatant is EnemyAI enemy) yield return StartCoroutine(EnemyTurn(enemy));
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
    } //why does build physical effects exist I feel like having the effects on the weapons should be enough for them to trigger right???
    IEnumerator StatusEffectsTimer(ICombatant combatant) 
    {
        List<ActiveStatusEffect> statuses = combatant.activeStatuses;
        if(statuses == null || statuses.Count == 0) yield break;
        List<ActiveStatusEffect> snapshot = new List<ActiveStatusEffect>(statuses);
        foreach(ActiveStatusEffect active in snapshot)
        {
            if(active.statusEffect == null) continue;
            yield return StartCoroutine(active.statusEffect.OnTimer(combatant));
            yield return StartCoroutine(CheckDeath(combatant));
            active.remainingTurns--;
            if(active.remainingTurns <= 0)
            {
                yield return StartCoroutine(active.statusEffect.OnExpire(combatant));
                statuses.Remove(active);
            }
        }
    }
    IEnumerator TurnStartEffects(ICombatant combatant)
    {
        if(combatant is ActiveStats gaugeOwner) gaugeOwner.GaugeTurn();
        yield return StartCoroutine(SkillEffects(combatant));
        yield return StartCoroutine(StatusEffectsTimer(combatant));
    }
    int AccuracyCheck(ICombatant attacker, ICombatant defender, DamageType type)
    {
        int atk, def;
        switch(type)
        {
            case DamageType.Magic: atk = attacker.Precision; def = defender.Foresight; break;
            case DamageType.Fusion: atk = (attacker.Accuracy + attacker.Precision) / 2; def = (defender.Evasion + defender.Foresight) / 2; break;
            default: atk = attacker.Accuracy; def = defender.Evasion; break;
        }
        return Mathf.Clamp(80 + atk - def, 0, 100);
    }
    int CritCheck(ICombatant attacker, ICombatant defender)
    {
        return Mathf.Clamp(attacker.Critical - defender.Dodge, 0, 100);
    }
    int ComputeDamage(ICombatant attacker, ICombatant defender, DamageType type, int bonusDamage, Element moveElement)
    {
        int raw; 
        switch(type)
        {
            case DamageType.Magic: raw = attacker.finalMagic - defender.finalWisdom; break;
            case DamageType.Fusion: raw = (attacker.finalStrength + attacker.finalMagic) / 2 - (defender.finalDefense + defender.finalWisdom) / 2; break;
            default: raw = attacker.finalStrength - defender.finalDefense; break;
        }
        int damage = Mathf.Max(1, raw + bonusDamage);
        if(moveElement != Element.None && elementChart != null)
        {
            float multiplier = elementChart.GetMultiplier(moveElement, defender.currentMagicAffinity);
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * multiplier));
        }
        return damage;
    }
    IEnumerator ResolveAttack(ICombatant attacker, ICombatant defender, DamageType type, int bonusDamage, string actionName, List<Effect> effects, Element moveElement)
    {
     int hitRate = AccuracyCheck(attacker, defender, type);
     int hitRoll = Random.Range(0, 100);
     if(hitRoll < hitRate)
        {
            int damage = ComputeDamage(attacker, defender, type, bonusDamage, moveElement);
            int critRate = CritCheck(attacker, defender);
            bool isCrit = Random.Range(0, 100) < critRate;
            if(isCrit) damage *= 3;
            defender.TakeDamage(damage);
            if(defender is EnemyAI hitEnemyAttack)
            {
                HitFlicker flicker = hitEnemyAttack.GetComponent<HitFlicker>();
                if(flicker != null) flicker.Flicker();
            }
            if(attacker is ActiveStats gainingAttacker) gainingAttacker.GaugeDamageDealt(damage);
            if(defender is ActiveStats gainingDefender) gainingDefender.GaugeDamageTaken(damage);
            UpdateUI();
            if(isCrit) yield return StartCoroutine(BattleTextBox.instance.ShowMessage("Critical Hit"));
            yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{attacker.currentName} uses {actionName}. {defender.currentName} takes {damage} damage."));
            yield return StartCoroutine(CheckDeath(defender));
            if(effects != null) foreach(Effect effect in effects) if (effect != null) yield return StartCoroutine(effect.Apply(attacker, defender));
        }   
        else
        {
            yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{attacker.currentName}'s {actionName} missed. "));
        }
    }
    IEnumerator ResolveAOE(ICombatant attacker, List<ICombatant> defenders, DamageType type, int bonusDamage, string actionName, List<Effect> effects, Element moveElement)
    {
        yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{attacker.currentName} uses {actionName} on all enemies"));
  foreach (ICombatant defender in defenders)
        {
            int hitRate = AccuracyCheck(attacker, defender, type);
     int hitRoll = Random.Range(0, 100);
     if(hitRoll < hitRate)
        {
            int damage = ComputeDamage(attacker, defender, type, bonusDamage, moveElement);
            int critRate = CritCheck(attacker, defender);
            bool isCrit = Random.Range(0, 100) < critRate;
            if(isCrit) damage *= 3;
            defender.TakeDamage(damage);
            if(defender is EnemyAI hitEnemyAOE)
            {
                HitFlicker flickerAOE = hitEnemyAOE.GetComponent<HitFlicker>();
                if(flickerAOE != null) flickerAOE.Flicker();
            }
            if(attacker is ActiveStats gainingAttacker) gainingAttacker.GaugeDamageDealt(damage);
            if(defender is ActiveStats gainingDefender) gainingDefender.GaugeDamageTaken(damage);
            UpdateUI();
            if(isCrit) yield return StartCoroutine(BattleTextBox.instance.ShowMessage("Critical Hit"));
            yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{attacker.currentName} uses {actionName}. {defender.currentName} takes {damage} damage."));
            yield return StartCoroutine(CheckDeath(defender));
            if(effects != null) foreach (Effect effect in effects) if (effect != null) yield return StartCoroutine(effect.Apply(attacker, defender));
        }   
        else
        {
            yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{attacker.currentName}'s {actionName} missed. "));
        }
        }  
    }
            IEnumerator PlayerAction(ActiveStats player, CommandMenu.PlayerActionType action, 
            ICombatant target, Learnable learnable, Item item)
        {
            switch (action)
            {
                case CommandMenu.PlayerActionType.Attack:
               if (target == null || target.currentHP <= 0) target = GetRandomEnemy();
               if (target != null) yield return StartCoroutine(ResolveAttack(player, target, DamageType.Physical, 0, "attacks", BuildPhysicalEffects(player.weaponSlot, null), Element.None));
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
                    int swapIndex = players.IndexOf(player);
                    if(swapIndex >= 0)
                    {
                        players[swapIndex] = incoming;
                        incoming.ResetTransformStat();
                        incoming.ResetStatStages();
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
                    runChance += currentPlayer.finalSpeed;
                   if (currentPlayer.currentLevel < averageLevel) runChance /= 2;
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
                   if(item.effects != null) foreach (Effect effect in item.effects) if(effect != null) StartCoroutine(effect.Apply(player, itemTarget));
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
                       yield return StartCoroutine(ResolveAOE(player, foes, DamageType.Physical, art.Damage, art.artName, BuildPhysicalEffects(player.weaponSlot, art.effects), Element.None));
                    }
                    else
                    {
                        if(target == null || target.currentHP <= 0) target = GetRandomEnemy();
                        if(target != null) yield return StartCoroutine(ResolveAttack(player, target, DamageType.Physical, art.Damage, art.artName, BuildPhysicalEffects(player.weaponSlot, art.effects), Element.None));
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
                    player.currentMagicAffinity = spell.element;
                player.currentMP = Mathf.Max(0, player.currentMP - spell.Cost);
                if (spell.isAOE)
                    {
                       List<ICombatant> foes = enemies.FindAll(enemy => enemy.currentHP > 0).Cast<ICombatant>().ToList();
                       yield return  StartCoroutine(ResolveAOE(player, foes, DamageType.Magic, spell.Damage, spell.spellName, spell.effects, spell.element));
                    }
                    else
                    {
                        if(target == null || target.currentHP <= 0) target = GetRandomEnemy();
                        if(target != null) yield return StartCoroutine(ResolveAttack(player, target, DamageType.Magic, spell.Damage, spell.spellName, spell.effects, spell.element));
                    }
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
                       yield return  StartCoroutine(ResolveAOE(player, foes, DamageType.Fusion, fusion.Damage, fusion.fusionName, fusion.effects, fusion.element));
                    }
                    else
                    {
                        if(target == null || target.currentHP <= 0) target = GetRandomEnemy();
                        if(target != null) yield return StartCoroutine(ResolveAttack(player, target, DamageType.Fusion, fusion.Damage, fusion.fusionName, fusion.effects, fusion.element));
                    }
                    break;
                    }
            }
            if(player.isTransformed) player.TransformTimer();
            UpdateUI();
            yield return StartCoroutine(ScaledWait(0.5f));
        } 
         IEnumerator EnemyTurn(EnemyAI enemy)
        {
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
                    StartCoroutine(ResolveAttack(enemy, target, DamageType.Physical, 0, "attacks", BuildPhysicalEffects(enemy.weaponSlot, null), Element.None));
                    else yield return StartCoroutine(EnemySpecial(enemy, target, move));
                }
                break;
                case EnemyAI.EnemyActionType.Defend:
                enemy.Defend();
                yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"{enemy.currentName} defends!"));
                break;
                case EnemyAI.EnemyActionType.Item:
                yield return StartCoroutine
                (BattleTextBox.instance.ShowMessage($"{enemy.currentName} uses item its neccesary to find out which though claude"));
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
        enemy.PayCost(learnable);
        List<ICombatant> livingPlayers = players.FindAll(player => player.currentHP > 0).Cast<ICombatant>().ToList();
        if(learnable is Art art)
        {
            if(art.isAOE) yield return StartCoroutine(ResolveAOE(enemy, livingPlayers, DamageType.Physical, art.Damage, art.artName, BuildPhysicalEffects(enemy.weaponSlot, art.effects), Element.None));
            else yield return StartCoroutine(ResolveAttack(enemy, target, DamageType.Physical, art.Damage, art.artName, BuildPhysicalEffects(enemy.weaponSlot, art.effects), Element.None));
        }
       else if(learnable is Spell spell)
        {
            enemy.currentMagicAffinity = spell.element;
            if(spell.isAOE) yield return StartCoroutine(ResolveAOE(enemy, livingPlayers, DamageType.Magic, spell.Damage, spell.spellName, spell.effects, spell.element));
            else yield return StartCoroutine(ResolveAttack(enemy, target, DamageType.Magic, spell.Damage, spell.spellName, spell.effects, spell.element));
        }
        if(learnable is Fusion fusion)
        {
            enemy.currentMagicAffinity = fusion.element;
            if(fusion.isAOE) yield return StartCoroutine(ResolveAOE(enemy, livingPlayers, DamageType.Fusion, fusion.Damage, fusion.fusionName, fusion.effects, fusion.element));
            else yield return StartCoroutine(ResolveAttack(enemy, target, DamageType.Fusion, fusion.Damage, fusion.fusionName, fusion.effects, fusion.element));
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
                SceneManager.LoadScene(RandomEncounter.previousSceneName);
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
        SceneManager.LoadScene(deathSceneName);
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
        foreach(ActiveStats player in livingPlayers)
        player.GainExperience(totalExp);
       for(int i = 0; i < livingPlayers.Count; i++)
        {
                for (int j = i + 1; j < livingPlayers.Count; j++)
                {
                    livingPlayers[i].AddBondPoints(livingPlayers[j].playerStats);
                    livingPlayers[j].AddBondPoints(livingPlayers[i].playerStats);
                }
            }
        if(totalGold > 0) WalletManager.instance.AddGold(totalGold);
        if(totalGold > 0) yield return StartCoroutine(BattleTextBox.instance.ShowMessage($"The party earns {totalGold} gold!"));
        foreach (EnemyStats.LootDrop drop in earnedDrops)
        {
            for (int q = 0; q < drop.quantity; q++)
            InventoryManager.instance.PickupItem(drop.item);
            yield return StartCoroutine(BattleTextBox.instance.ShowMessage($" Found {drop.item.itemName} x{drop.quantity}"));
        }
    }
}