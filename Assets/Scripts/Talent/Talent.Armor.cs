public static partial class Talents {

    public static Talent Armor() {
        return new() {
            StatIncrease = new() {
                statType = Stats.ArmorID, 
                value = 1f
            },
            Name = "Armor",
            Icon = "Armor",
            TalentDescType = TalentDescType.Player,
            MaximumRank = 5,
            CostPerRank = 500,
        };
    }
}