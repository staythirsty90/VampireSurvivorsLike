public static partial class PowerUps {
    public static PowerUp HealthRegen() {
        return new() {
            name = "Recovery",
            maxLevel = 5,
            description = "Regenerates Health every second.",
            _isWeapon = false,
            affectedStats = new() {
                new() {
                    statType = Stats.HealthRegenerationID,
                    value = 0.2f,
                },
            },
            spriteName = "HealthRegen",
            rarity = 50
        };
    }
}