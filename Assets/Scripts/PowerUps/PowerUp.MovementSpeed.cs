public static partial class PowerUps {
    public static PowerUp MovementSpeed() {
        return new() {
            name = "Boots of Speed",
            maxLevel = 5,
            _isWeapon = false,
            affectedStats = new() {
                new() {
                    statType = Stats.MovementSpeedID,
                    value = 10,
                    isPercentageBased = true,
                },
            },
            spriteName = "MoveSpeed",
            rarity = 70
        };
    }
}