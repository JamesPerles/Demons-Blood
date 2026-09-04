using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
public class QuestMenu : MonoBehaviour, ICardHighlightHandler, IPageableTab
{
public PauseMenu host;
public GameObject questCardPrefab;
public Transform questCardParent;
public Color cardBorderDefault = new Color32(0x3A, 0x16, 0x16, 0xFF);
public Color cardBorderSelected = new Color32(0xD8, 0x5A, 0x30, 0xFF);
public Color cardBackgroundSelected = new Color32(0x24, 0x10, 0x10, 0xFF);
public Color cardTitleDefault = new Color32(0xC9, 0xC2, 0xC2, 0xFF);
public Color cardTitleSelected = new Color32(0xF2, 0xF2, 0xF2, 0xFF);
const string colorMuted = "#8A8580";
const string colorBody = "#C9C2C2";
const string colorBright = "#E8E4E0";
const string colorDone = "#639922";
const string colorPending = "#5F5E5A";
const string dot = "\u00B7";
const string checkMark = "\u2713";
const string circleMark = "\u25CB";
GridCardPager pager;
List<QuestProgress> currentActiveList = new List<QuestProgress>();
List<Quest> currentCompletedList = new List<Quest>();
bool showingCompleted = false;
public void OpenTab()
    {
        host.PrepareTabSwitch();
        if(pager == null) pager = new GridCardPager(questCardPrefab, questCardParent, host, 3, 3);
        host.ShowSplitPanel();
        host.SetCardHighlightHandler(this);
        host.SetPageableTab(this);
        SetupMiniTabs();
    }
    void SetupMiniTabs()
    {
        if(host.miniTabGroup == null) return;
        List<TabDefinition> tabs = new List<TabDefinition>
        {
            new TabDefinition("Main", () => ShowQuestList(true)),
            new TabDefinition("Side", () => ShowQuestList(false)),
            new TabDefinition("Completed", ShowCompletedList),
        };
        host.miniTabGroup.SetTabs(tabs, 0);
    }
void ShowQuestList(bool mainQuests)
    {
        showingCompleted = false;
        host.SetBreadcrumbSuffix(mainQuests ? "Quests > Main" : "Quests > Side");
        currentActiveList.Clear();
        if(QuestManager.instance != null)
        foreach(QuestProgress progress in QuestManager.instance.activeQuests)
        if(progress.quest.isMainQuest == mainQuests) currentActiveList.Add(progress);
        RebuildCards();
    }
    void ShowCompletedList()
    {
        showingCompleted = true;
        host.SetBreadcrumbSuffix("Quests > Completed");
        currentCompletedList.Clear();
        if(QuestManager.instance != null) currentCompletedList.AddRange(QuestManager.instance.completedQuests);
        RebuildCards();
    }
    void RebuildCards()
    {
        if(pager == null || questCardPrefab == null || questCardParent == null) return;
           List<CardGridSpec> specs = new List<CardGridSpec>();
            if(showingCompleted)
            {
                foreach(Quest quest in currentCompletedList)
                {
                    Quest captured = quest;
                    specs.Add(new CardGridSpec(quest.questName, "Completed", BuildCompletedDetail(captured), () => { }));
                }
            }
            else
            {
            foreach(QuestProgress progress in currentActiveList)
            {
                QuestProgress captured = progress;
                specs.Add(new CardGridSpec(progress.quest.questName, BuildProgressSubText(progress), BuildActiveDetail(captured), () => { }));
            }
        }
        pager.SetSpecs(specs);
        if(pager.SpawnedCards.Count > 0) host.EntryHighlight(pager.SpawnedCards[0]);
        else if(host.detailText != null) host.detailText.text = showingCompleted ? "No completed quests yet." : "No quests in this category";
    }
    public void NextPage()
    {
        pager?.NextPage();
        if(pager != null && pager.SpawnedCards.Count > 0) host.EntryHighlight(pager.SpawnedCards[0]);
    }
    public void PreviousPage()
    {
        pager?.PreviousPage();
        if(pager != null && pager.SpawnedCards.Count > 0) host.EntryHighlight(pager.SpawnedCards[0]);
    }
    string BuildProgressSubText(QuestProgress progress)
    {
        QuestStage stage = progress.CurrentStage;
        if(stage == null) return "";
        if(stage.isChoiceStage)
        {
            bool chosen = progress.GetChosenObjective(progress.currentStage) >= 0;
            return chosen ? "1 / 1 objectives" : "0 / 1 objectives";
        }
        int satisfied = 0;
        for(int i = 0; i < stage.objectives.Count; i++)
        if(QuestManager.instance != null && QuestManager.instance.IsObjectiveSatisfied(progress, i)) satisfied++;
        return $"{satisfied} / {stage.objectives.Count} objectives";
    }
    string BuildActiveDetail(QuestProgress progress)
    {
        Quest quest = progress.quest;
        QuestStage stage = progress.CurrentStage;
        string subtitle = quest.isMainQuest ? "Main Quest" : "Side Quest";
        if(!string.IsNullOrEmpty(quest.questGiver)) subtitle += $" {dot} given by {quest.questGiver}";
        string objectives = "";
        if(stage != null)
        {
            if(stage.isChoiceStage)
            {
                int chosen = progress.GetChosenObjective(progress.currentStage);
                if(chosen >= 0 && chosen < stage.objectives.Count)
                {
                    objectives = $"color={colorDone}>{checkMark}</color> {stage.objectives[chosen].description}\n";
                }
                else
                {
                List<string> choiceParts = new List<string>();
                foreach(QuestObjective obj in stage.objectives) choiceParts.Add(obj.description);
                objectives = $"<color={colorPending}>{circleMark}</color>" + string.Join(" OR", choiceParts) + "\n";
                }
            }
            else
            {
                for(int i = 0; i < stage.objectives.Count; i++)
                {
                    bool done = QuestManager.instance != null && QuestManager.instance.IsObjectiveSatisfied(progress, i);
                    string mark = done ? $"<color={colorDone}>{checkMark}</color>" : $"<color={colorPending}>{circleMark}</color>";
                    objectives += $"{mark} {stage.objectives[i].description}\n";
                }
            }
        }
        string reward = BuildRewardLine(quest.goldReward, quest.expReward, quest.itemRewards, quest.equipmentRewards);
        return BuildDetailBlock(quest.questName, subtitle, quest.description, objectives, reward);
    }
    string BuildCompletedDetail(Quest quest)
    {
        string subtitle = (quest.isMainQuest ? "Main Quest" : "Side Quest") + " {dot}; completed";
        string reward = BuildRewardLine(quest.goldReward, quest.expReward, quest.itemRewards, quest.equipmentRewards);
        return BuildDetailBlock(quest.questName, subtitle, quest.description, "", reward);
    }
    string BuildDetailBlock(string title, string subtitle, string description, string objectivesBlock, string reward)
    {
        string result = $"<size=140%><color=#F2F2F2>{title}</color></size>\n";
        result += $"<size=80%><color={colorMuted}>{subtitle}</color></size>\n\n";
        result += $"<color={colorBody}>{description}</color>\n\n";
        if(!string.IsNullOrEmpty(objectivesBlock))
        {
            result += $"<size=75%><color={colorMuted}>OBJECTIVES</color></size>\n";
            result += $"<color={colorBright}>{objectivesBlock}</color>\n";
        }
            result += $"<size=75%><color={colorMuted}>REWARD</color></size>\n";
            result += $"<color={colorBright}>{reward}</color>";
            return result;
        }
        string BuildRewardLine(int gold, int exp, List<Item> items, List<Equipment> equipment)
    {
        List<string> parts = new List<string>();
        if(gold > 0) parts.Add($"{gold} gold");
        if(exp > 0) parts.Add($"{exp} exp");
        if(items != null) foreach(Item item in items) if(item != null) parts.Add(item.name);
        if(equipment != null) foreach(Equipment equip in equipment) if(equip != null) parts.Add(equip.name);
        return parts.Count > 0 ? string.Join($" {dot}; ", parts) : "None";
    }
        public void OnCardHighlighted(GameObject entry)
    {
        if(pager == null) return;
        for(int i = 0; i < pager.SpawnedCards.Count; i++)
        {
            if(pager.SpawnedCards[i] == null) continue;
            EntryCard card = pager.SpawnedCards[i].GetComponent<EntryCard>();
            if(card == null) continue;
            SetCardVisual(card, pager.SpawnedCards[i] == entry);
        }
    }
    void SetCardVisual(EntryCard card, bool selected)
    {
        if(card.borderImage != null) card.borderImage.color = selected ? cardBorderSelected : cardBorderDefault;
        if(card.backgroundImage != null)
        {
            Color bg = cardBackgroundSelected;
            bg.a = selected ? 1f : 0f;
            card.backgroundImage.color = bg;
        }
        if(card.titleText != null) card.titleText.color = selected ? cardTitleSelected : cardTitleDefault;
    }
    }
