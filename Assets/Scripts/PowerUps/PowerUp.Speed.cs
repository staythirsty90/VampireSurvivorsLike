public static partial class PowerUps {
    public static PowerUp Speed() {
        return new() {
            name = "Accel",
            maxLevel = 5,
            _isWeapon = false,
            affectedStats = new() {
                new() {
                    statType = Stats.SpeedID,
                    value = 10,
                    isPercentageBased = true,
                },
            },
            spriteName = "Speed",
            rarity = 50
        };
    }
}