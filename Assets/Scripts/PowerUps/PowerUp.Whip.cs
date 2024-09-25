public static partial class PowerUps {
    public static PowerUp Whip() {
        return new() {
            name = "Whip",
            maxLevel = 8,
            description = "A Whip!",
            spriteName = "Whip",
            rarity = 100,
            _isWeapon = true,

            baseStats = new() {
                Cooldown = 1.35f,
                Amount = 1,
                Interval = 0.1f,
            },

            growthStats = new() {
                new() { Cooldown = -0.2f },
                new() { Cooldown = -0.2f },
                new() { Amount = 1, },
                new() { Cooldown = -0.2f },
                new() { Cooldown = -0.2f },
                new() { Amount = 1, },
                new() { Cooldown = -0.2f },
            },

            Weapons = new() {
                new() {
                    PowerUpShootType = ShootType.Interval,
                    missileData = new() {
                        missileID = Missiles.WhipID,
                        SpawnVariationType = SpawnVariationType.FlipsUpward,
                        SpawnDirection = Directions.None,
                    }
                }
            }
        };
    }
}