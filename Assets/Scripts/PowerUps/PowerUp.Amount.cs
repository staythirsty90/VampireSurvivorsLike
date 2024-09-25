public static partial class PowerUps {
    public static PowerUp Amount() {
        return new() {
            name = "Stone of Jordan",
            maxLevel = 2,
            spriteName = "SoJ",
            _isWeapon = false,
            affectedStats = new() {
                new() {
                    statType = Stats.AmountID,
                    value = 1
                },
            },
            rarity = 25,
        };
    }
}