public static partial class PowerUps {
    public static PowerUp Frostbolt() {
        return new() {
            name = "Frostbolt",
            evolutionName = FrostboltEvolved().name,
            maxLevel = 8,
            description = "Fires a bolt of Ice to damage and freeze nearby enemies",
            spriteName = "Frostbolt",
            rarity = 100,
            _isWeapon = true,

            baseStats = new() {
                Cooldown = 3f,
                Amount = 1,
                Interval = 0.02f,
            },

            growthStats = new() {
                new() { Damage = 10 },
                new() { Damage = 10, Speed = 0.20f },
                new() { Damage = 10 },
                new() { Damage = 10, Speed = 0.20f },
                new() { Piercing = 1 },
                new() { Damage = 10, Speed = 0.20f },
                new() { Damage = 10 },
            },
            

            Weapons = new() {
                new() {
                    PowerUpShootType = ShootType.Interval,
                    missileData = new() {
                        SpawnDirection = Directions.Angles_5_to_45,
                        missileID = Missiles.FrostboltID
                    }
                }
            },
        };
    }
}