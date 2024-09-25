public static partial class Talents {

    public static Talent Duration() {
        return new() {
            StatIncrease = new() {
                statType = Stats.DurationID, 
                isPercentageBased = true,
                value = 2.5f
            },
            Name = "Duration",
            Icon = "Duration",
            TalentDescType = TalentDescType.Missile,
            MaximumRank = 5,
            CostPerRank = 500,
        };
    }
}