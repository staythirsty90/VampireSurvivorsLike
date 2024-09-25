public static partial class PowerUps {
    public static PowerUp Greed() {
        return new() {
            name = "Gold Mask",
            maxLevel = 5,
            _isWeapon = false,
            affectedStats = new() {
                new() {
                    statType = Stats.GreedID,
                    value = 10,
                    isPercentageBased = true,
                },
            },
            spriteName = "GoldMask",
            rarity = 50
        };
    }
}