public static partial class PowerUps {
    public static PowerUp Duration() {
        return new() {
            name = "Hour Glass",
            maxLevel = 5,
            _isWeapon = false,
            affectedStats = new() {
                new() {
                    statType = Stats.DurationID,
                    value = 10,
                    isPercentageBased = true,
                },
            },
            spriteName = "Duration",
            rarity = 50
        };
    }
}