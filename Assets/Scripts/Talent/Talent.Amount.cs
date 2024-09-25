public static partial class Talents {

    public static Talent Amount() {
        return new() {
            StatIncrease = new() {
                statType = Stats.AmountID,
                value = 1f
            },
            Name = "Amount",
            Icon = "SoJ",
            TalentDescType = TalentDescType.Missile,
            MaximumRank = 2,
            CostPerRank = 2500,
        };
    }
}