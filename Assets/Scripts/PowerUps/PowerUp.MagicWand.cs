public static partial class PowerUps {
    public static PowerUp MagicWand() {
        return new() {
            name = "Magic Wand",
            maxLevel = 8,
            description = "A Magic Wand!",
            spriteName = "MagicWand",
            rarity = 100,
            _isWeapon = true,

            baseStats = new() {
                Cooldown = 1.2f,
                Amount = 1,
                Interval = 0.1f,
            },

            growthStats = new() {
                new() { Amount   = 1 },
                new() { Cooldown = -0.2f },
                new() { Amount   = 1 },
                new() { Damage   = 10 },
                new() { Amount   = 1 },
                new() { Piercing = 1 },
                new() { Damage   = 10 },
            },

            Weapons = new() {
                new() {
                    PowerUpShootType = ShootType.Interval,
                    missileData = new() {
                        SpawnDirection = Directions.NearestEnemyDirection,
                        SpawnVariationType = SpawnVariationType.RandomSpread,
                        missileID = Missiles.MagicMissileID
                    }
                }
            },
        };
    }
}