public static partial class Talents {

    public static Talent CooldownReduction() {
        return new() {
            StatIncrease = new() {
                statType = Stats.CooldownReductionID, 
                isPercentageBased = true,
                value = 2.5f
            },
            Name = "Cooldown Reduction",
            Icon = "CooldownReduction",
            TalentDescType = TalentDescType.Missile,
            MaximumRank = 5,
            CostPerRank = 500,
        };
    }
}