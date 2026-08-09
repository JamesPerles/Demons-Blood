using System.Collections.Generic;
using UnityEngine;
public class MapManager : SubMenu
{
    public static MapManager instance;
    public List<TownData> allTowns = new List<TownData>();
    void Awake()
    {
        if(instance == null) instance = this; else Destroy(gameObject);
    }
    void Start()
    {
        SetDisplayActive(false);
    }
    public void Open()
    {
        SetDisplayActive(true);
        screenHistory.Clear();
        OpenScreen(WorldMenu());
    }
    public override void Close()
    {
        SetDisplayActive(false);
        ClearEntries();
        screenHistory.Clear();
    }
    List<MenuOption> WorldMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach(TownData town in allTowns)
        {
            if(town == null || !town.unlockedForFastTravel) continue;
            TownData captured = town;
            options.Add(new MenuOption(captured.townName, () => TownMenu(captured)));
        }
        return options;
    }
    List<MenuOption> TownMenu(TownData town)
    {
        List<MenuOption> options = new List<MenuOption>();
        options.Add(new MenuOption(town.description, () => { }) {enabled = false});
        options.Add(new MenuOption("Shops", () => ShopListMenu(town)));
        options.Add(new MenuOption("NPCs", () => NpcListMenu(town)));
        options.Add(new MenuOption("Quests", () => QuestListMenu(town)));
        return options;
    }
    List<MenuOption> ShopListMenu(TownData town)
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach(ShopStats shop in town.shops)
        {
            if(shop == null) continue;
            options.Add(new MenuOption(shop.shopName, () => { }) { enabled = false});
        }
        return options;
    }
    List<MenuOption> NpcListMenu(TownData town)
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach(NPC npc in town.npcs)
        {
        if(npc == null) continue;
        options.Add(new MenuOption(npc.npcName, () => { }){enabled = false});    
        }
        return options;
    }
    List<MenuOption> QuestListMenu(TownData town)
    {
        List<MenuOption> options = new List<MenuOption>();
        if(QuestManager.instance == null) return options;
        foreach(Quest quest in town.associatedQuests)
        {
            if(quest == null) continue;
            string status;
        if(QuestManager.instance.completedQuests.Contains(quest)) status = "Completed";
        else if(QuestManager.instance.activeQuests.Exists(progress => progress.quest == quest)) status = "Active";
        else if(QuestManager.instance.IsQuestAvailable(quest)) status = "Available";
        else continue;
        options.Add(new MenuOption($"{quest.questName} ({status})", () => { }) { enabled = false});
        }
        return options;
    }
    }
