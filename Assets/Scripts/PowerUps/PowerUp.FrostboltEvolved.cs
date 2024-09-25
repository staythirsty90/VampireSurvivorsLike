public static partial class PowerUps {
    public static PowerUp FrostboltEvolved() {
        return new() {
            name = "Frozen Orb",
            requiresName = Speed().name,
            maxLevel = 1,
            description = "Has no cooldown.",
            spriteName = "FrozenOrb",
            isEvolution = true,
            _isWeapon = true,

            baseStats = new() {
                Cooldown = 7f,
                Amount = 1,
                Interval = 11f,
            },

            Weapons = new() {
                new() {
                    PowerUpShootType = ShootType.Interval,
                    missileData = new() {
                        missileID = Missiles.FrozenOrbID,
                        SpawnDirection = Directions.NearestEnemyDirection,
                    }
                },
                new() {
                    PowerUpShootType = ShootType.Interval,
                    missileData = new() {
                        missileID = Missiles.FrozenOrbFrostboltID,
                        SpawnPositionType = SpawnPositionType.AtFamiliar,
                        SpawnVariationType = SpawnVariationType.FamiliarRandomSpread,
                        SpawnDirection = Directions.RandomDirection,
                    },
                    baseStats = new() {
                        Interval = 0.01f,
                        Amount = 300,
                        Cooldown = 15,
                    },
                }
            },
        };
    }
}