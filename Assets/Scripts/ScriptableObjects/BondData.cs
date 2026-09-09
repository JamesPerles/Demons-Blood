using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class StatBonus
{
    public Stat stat;
    public int amount;
}
public enum BondRank {None, C, B, A}
[System.Serializable]
public class BondRankBonus
{
        public BondRank rank;
        public List<StatBonus> bonuses = new List<StatBonus>();
    }
    [System.Serializable]
    public class BondConversation
{
    public BondRank requiredRank;
    [TextArea] public string dialogue;
}
[System.Serializable]
public class BondData
{
    public PlayerStats partner;
    public List<BondRankBonus> rankBonuses = new List<BondRankBonus>();
    public List<BondConversation> conversations = new List<BondConversation>();
}
[System.Serializable]
public class BondProgress
{
    public PlayerStats partner;
    public int points;
    public List<bool> conversationsViewed = new List<bool>();
}
public static class BondSettings
{
    public const int CRank = 10;
    public const int BRank = 25;
    public const int ARank = 50;
    public static int RequiredPoints(BondRank rank)
    {
        switch(rank)
        {
            case BondRank.C: return CRank;
            case BondRank.B: return BRank;
            case BondRank.A: return ARank;
            default: return 0;
        }
    }
}
