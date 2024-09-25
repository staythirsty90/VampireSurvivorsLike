public static partial class PowerUps {
    public static PowerUp Bone() {
        return new() {
            name = "Bone",
            maxLevel = 8,
            description = "Bounces off walls and enemies",
            spriteName = "bonePowerUp",
            rarity = 100,
            _isWeapon = true,

            baseStats = new() {
                Cooldown = 3f,
                Amount = 1,
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
                        SpawnDirection = Directions.RandomEnemyDirection,
                        missileID = Missiles.BoneID,
                    }
                }
            },
        };
    }
}