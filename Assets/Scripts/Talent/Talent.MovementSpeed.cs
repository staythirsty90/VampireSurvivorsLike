public static partial class Talents {

    public static Talent MovementSpeed() {
        return new() {
            StatIncrease = new() {
                statType = Stats.MovementSpeedID, 
                isPercentageBased = true, 
                value = 10f
            },
            Name = "Movement Speed",
            Icon = "MoveSpeed",
            TalentDescType = TalentDescType.Player,
            MaximumRank = 5,
            CostPerRank = 500,
        };
    }
}