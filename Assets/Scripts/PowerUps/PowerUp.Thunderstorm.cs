public static partial class PowerUps {
    public static PowerUp Thunderstorm() {
        return new PowerUp() {
            name = "Thunderstorm",
            maxLevel = 8,
            spriteName = "Lightning",
            rarity = 100,
            _isWeapon = true,

            baseStats = new() {
                Cooldown = 4.5f,
                Amount = 2,
                Interval = 0.05f,
            },

            description = "Summon a powerful tempest.",

            growthStats = new() {
                new() { Cooldown = -0.1f },
                new() { Cooldown = -0.1f },
                new() { Amount = 1, },
                new() { Cooldown = -0.1f },
                new() { Cooldown = -0.1f },
                new() { Amount = 1, },
                new() { Cooldown = -0.1f },

            },

            Weapons = new() {
                new() {
                    PowerUpShootType = ShootType.Interval,
                    missileData = new() {
                        SpawnPositionType = SpawnPositionType.AtRandomEnemy,
                        missileID = Missiles.LightningID
                    }
                } 
            },
        };
    }
}