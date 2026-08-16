using UnityEngine;
public enum Stat {HP, MP, Strength, Magic, Defense, Wisdom, Tech, Affinity, Speed, Luck, Accuracy, Evasion, Precision, Foresight, Critical, Dodge}
public static class StatStageUtility
{
    public const int MaxStage = 4;
    public const int MinStage = -4;
    public static float Multiplier(int stage)
    {
        stage = Mathf.Clamp(stage, MinStage, MaxStage);
        return stage >= 0 ? 1f + 0.25f * stage : 1f / (1f + 0.25f * -stage);
    }
}
