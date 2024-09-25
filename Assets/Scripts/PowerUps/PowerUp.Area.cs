public static partial class PowerUps {
    public static PowerUp Area() {
        return new() {
            name = "Augment",
            maxLevel = 5,
            _isWeapon = false,
            affectedStats = new() {
                new() {
                    statType = Stats.AreaID,
                    value = 10,
                    isPercentageBased = true,
                },
            },
            spriteName = "Area",
            rarity = 50
        };
    }
}