public static partial class PowerUps {
    public static PowerUp Armor() {
        return new() {
            name = "Armor",
            rarity = 100,
            maxLevel = 5,
            _isWeapon = false,
            spriteName = "Armor",
            affectedStats = new() {
                new() { statType = Stats.ArmorID, value = 1f, },
            },
        };
    }
}