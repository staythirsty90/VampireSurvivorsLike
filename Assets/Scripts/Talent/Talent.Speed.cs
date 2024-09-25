public static partial class Talents {

    public static Talent Speed() {
        return new() {
            StatIncrease = new() {
                statType = Stats.SpeedID, 
                isPercentageBased = true,
                value = 10f
            },
            Name = "Speed",
            Icon = "Speed",
            TalentDescType = TalentDescType.Missile,
            MaximumRank = 5,
            CostPerRank = 500,
        };
    }
}