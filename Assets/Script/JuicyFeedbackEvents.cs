using System;

public enum JuicyFeedbackType
{
    MotherSeedClick,
    PlantSpore,
    PlantPowerUp,
    TrophyAchieved,
    CrystalDispensed
}

public static class JuicyFeedbackEvents
{
    public static event Action<JuicyFeedbackType> Happened;

    public static void Raise(JuicyFeedbackType feedbackType)
    {
        Happened?.Invoke(feedbackType);
    }
}
