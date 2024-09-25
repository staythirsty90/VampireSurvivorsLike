public static partial class Talents {

    public static Talent Area() {
        return new() {
            StatIncrease = new() {
                statType = Stats.AreaID, 
                isPercentageBased = true, 
                value = 10f
            },
            Name = "Area",
            Icon = "Area",
            TalentDescType = TalentDescType.Missile,
            MaximumRank = 5,
            CostPerRank = 500,
        };
    }
}