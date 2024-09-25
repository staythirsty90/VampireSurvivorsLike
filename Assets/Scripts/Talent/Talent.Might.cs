public static partial class Talents {

    public static Talent Might() {
        return new() {
            StatIncrease = new() {
                statType = Stats.MightID, 
                value = 1f
            },
            Name = "Might",
            Icon = "Might",
            TalentDescType = TalentDescType.Player,
            MaximumRank = 5,
            CostPerRank = 500,
        };
    }
}