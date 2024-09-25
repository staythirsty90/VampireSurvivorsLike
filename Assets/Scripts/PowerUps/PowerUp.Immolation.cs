public static partial class PowerUps {
    public static PowerUp Immolation() {
        return new() {
            name = "Immolation",
            maxLevel = 8,
            description = "Engulfs the Player with flame damaging nearby enemies",
            spriteName = "Immolation",
            rarity = 100,
            _isWeapon = true,

            baseStats = new() {
                Amount = 1,
            },

            growthStats = new() {
                new() { Area = 0.5f },
                new() { Damage = 1 },
                new() { Area = 0.5f },
                new() { Damage = 1 },
                new() { Area = 0.5f },
                new() { Damage = 1 },
                new() { Area = 0.5f },
            },
            ParticleSystemGameObjectName = "Audio Source Immolation",
            
            Weapons = new() {
                new() {
                    PowerUpShootType = ShootType.FiresOnlyOnceEver,
                    missileData = new() {
                        missileID = Missiles.ImmolationID,
                    }
                }
            }
        };
    }
}