public static partial class PowerUps {
    public static PowerUp GrabRange() {
        return new() {
            name = "Vortex",
            maxLevel = 5,
            _isWeapon = false,
            affectedStats = new() {
                new() {
                    statType = Stats.PickUpGrabRangeID,
                    value = 1,
                },
            },
            spriteName = "Range",
            rarity = 70
        };
    }
}