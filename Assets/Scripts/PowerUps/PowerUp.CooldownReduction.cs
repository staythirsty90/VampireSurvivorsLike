public static partial class PowerUps {
    public static PowerUp CooldownReduction() {
        return new() {
            name = "Chrono",
            maxLevel = 5,
            _isWeapon = false,
            affectedStats = new() {
                new() {
                    statType = Stats.CooldownReductionID,
                    value = 8,
                    isPercentageBased = true,
                },
            },
            spriteName = "CooldownReduction",
            rarity = 35
        };
    }
}