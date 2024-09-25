public static partial class PowerUps {
    public static PowerUp Growth() {
        return new() {
            name = "Growth",
            maxLevel = 5,
            _isWeapon = false,
            affectedStats = new() {
                new() {
                    statType = Stats.GrowthID,
                    value = 8,
                    isPercentageBased = true,
                },
            },
            spriteName = "GoldCrown",
            rarity = 50
        };
    }
}