public static partial class PowerUps {
    public static PowerUp Shield() {
        return new() {
            name = "Divine Shield",
            maxLevel = 8,
            description = "Protects from Damage while active",
            PowerUpType = PowerUpType.ChargedBuff,
            baseStats = new() {
                Cooldown = 15,
                Amount = 1,
            },
            _isWeapon = true,

            growthStats = new() {
                new() { Cooldown = -0.5f, Duration = 0.2f },
                new() { Cooldown = -0.5f, Duration = 0.2f },
                new() { Amount = 1 },
                new() { Cooldown = -0.5f, Duration = 0.2f },
                new() { Cooldown = -0.5f, Duration = 0.2f },
                new() { Amount = 1 },
                new() { Cooldown = -0.5f, Duration = 0.2f },
            },

            spriteName = "Shield",
            rarity = 50,

            Weapons = new() {
                new() {
                    PowerUpShootType = ShootType.ActivatesOnPlayerHit,
                    missileData = new() {
                        missileID = Missiles.DivineShieldID,
                        SpawnDirection = Directions.None,

                    }
                }
            },
        };
    }
}