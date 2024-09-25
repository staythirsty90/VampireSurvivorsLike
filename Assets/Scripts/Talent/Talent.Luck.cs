public static partial class Talents {

    public static Talent Luck() {
        return new() {
            StatIncrease = new() {
                statType = Stats.LuckID, 
                isPercentageBased = true,
                value = 5f
            },
            Name = "Luck",
            Icon = "Clover",
            TalentDescType = TalentDescType.Player,
            MaximumRank = 5,
            CostPerRank = 500,
        };
    }
}