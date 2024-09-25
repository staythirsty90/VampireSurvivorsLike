public static partial class PowerUps {
    public static PowerUp Boomerang() {
        return new() {
            name = "Boomerang",
            maxLevel = 8,
            description = "Aims at Nearest Enemy like a boomerang.",
            spriteName = "boomerangPowerUp",
            rarity = 100,
            _isWeapon = true,

            baseStats = new() {
                Cooldown = 2f,
                Amount = 1,
                Interval = 0.1f,
            },
            growthStats = new() {
                new() { Damage   = 10 },
                new() { Area     = 0.1f, Speed = 0.25f },
                new() { Amount   = 1 },
                new() { Damage   = 10 },
                new() { Area     = 0.1f, Speed = 0.25f },
                new() { Amount   = 1 },
                new() { Damage   = 10 },
            },


            Weapons = new() {
                new() {
                    PowerUpShootType = ShootType.Interval,
                    missileData = new() {
                        SpawnDirection = Directions.NearestEnemyDirection,
                        SpawnVariationType = SpawnVariationType.RandomSpreadWithArea2_Cross,
                        missileID = Missiles.BoomerangID,
                    }
                }
            }
        };
    }
}