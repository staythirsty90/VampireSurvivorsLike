public static partial class PowerUps {
    public static PowerUp Runetracer() {
        return new() {
            name = "Runetracer",
            maxLevel = 8,
            description = "Bounces off walls and pierces through enemies",
            spriteName = "Runetracer",
            rarity = 100,
            _isWeapon = true,

            baseStats = new() {
                Cooldown = 3f,
                Amount = 1,
                Interval = 0.2f,
            },

            growthStats = new() {
                new() { Damage = 5, Speed = 0.2f },
                new() { Damage = 5, Duration = 0.3f },
                new() { Amount = 1, },
                new() { Damage = 5, Speed = 0.2f },
                new() { Damage = 5, Duration = 0.3f },
                new() { Amount = 1, },
                new() { Duration = 0.5f },
            },

            Weapons = new() {
                new() {
                    PowerUpShootType = ShootType.Interval,
                    missileData = new() {
                        missileID = Missiles.RunetracerID,
                        SpawnDirection = Directions.Angles_10_to_40,
                    }
                }
            },
        };
    }
}