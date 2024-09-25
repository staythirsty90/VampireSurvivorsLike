public static partial class Talents {

    public static Talent PickUpGrabRange() {
        return new() {
            StatIncrease = new() {
                statType = Stats.PickUpGrabRangeID, 
                isPercentageBased = true,
                value = 5f
            },
            Name = "PickUp Grab Range",
            Icon = "Range",
            TalentDescType = TalentDescType.Player,
            MaximumRank = 5,
            CostPerRank = 750,
        };
    }
}