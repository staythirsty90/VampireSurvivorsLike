public static partial class PowerUps {
    public static PowerUp Luck() {
        return new() {
            name = "Clover",
            maxLevel = 5,
            _isWeapon = false,
            affectedStats = new() {
                new() {
                    statType = Stats.LuckID,
                    value = 8,
                    isPercentageBased = true,
                },
            },
            spriteName = "Clover",
            rarity = 25
        };
    }
}