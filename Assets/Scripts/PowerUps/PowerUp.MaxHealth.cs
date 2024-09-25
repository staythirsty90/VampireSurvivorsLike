public static partial class PowerUps {
    public static PowerUp MaximumHealth() {
        return new() {
            name = "Big Heart",
            maxLevel = 5,
            _isWeapon = false,
            affectedStats = new() {
                new() {
                    statType = Stats.MaxHealthID,
                    value = 20,
                    isPercentageBased = true,
                },
            },
            spriteName = "MaxHealth",
            rarity = 50
        };
    }
}