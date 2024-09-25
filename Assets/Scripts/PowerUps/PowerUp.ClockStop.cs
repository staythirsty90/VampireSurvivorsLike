public static partial class PowerUps {
    public static PowerUp ClockStop() {
        return new() {
            name = "Clock Lance",
            maxLevel = 7,
            description = "Stops hit enemies in its tracks.",
            spriteName = "ClockStop",
            rarity = 100,
            _isWeapon = true,

            baseStats = new() {
                Cooldown = 2f,
                Amount = 1,
                Interval = 1f,
            },

            growthStats = new() {
                new() { FreezeTime = 1f },
                new() { Cooldown = -0.5f },
                new() { FreezeTime = 1f },
                new() { FreezeTime = 1f },
                new() { Cooldown = -0.5f },
                new() { FreezeTime = 1f },
                // only has 7 levels...
            },

            Weapons = new() {
                new() {
                    PowerUpShootType = ShootType.Interval,
                    missileData = new() {
                        SpawnDirection = Directions.Clockwise,
                        missileID = Missiles.ClockStopID,
                    }
                }
            },
        };
    }
}