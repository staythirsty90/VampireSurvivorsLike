public static partial class PowerUps {
    public static PowerUp Might() {
        return new() {
            name = "Blessing of Might",
            maxLevel = 5,
            _isWeapon = false,
            affectedStats = new() {
                new() {
                    statType = Stats.MightID,
                    value = 5,
                },
            },
            spriteName = "Might",
            rarity = 100
        };
    }
}