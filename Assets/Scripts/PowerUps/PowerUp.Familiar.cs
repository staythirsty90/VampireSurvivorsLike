using Unity.Mathematics;

public static partial class PowerUps {
    public static PowerUp Familiar() {
        return new() {
            name = "Pigeon",
            maxLevel = 8,
            description = "Summon a Pigeon to assist you in combat!",
            spriteName = "birdPowerUp",
            rarity = 100,
            _isWeapon = true,

            baseStats = new() {
                Cooldown = 6f,
                Amount = 1,
                Interval = 0.05f,
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

            Weapons = new() {
                new() {
                    PowerUpShootType = ShootType.FiresOnlyOnceEver,
                    spawnOffset = new float3(0,0,-1f),
                    missileData = new() {
                        missileID = Missiles.FamiliarID,
                        SpawnDirection = Directions.None,
                    }
                },
                new() {
                    PowerUpShootType = ShootType.FiresOnlyOnceEver,
                    missileData = new() {
                        missileID = Missiles.FamiliarDamageZoneID,
                        SpawnDirection = Directions.None,
                    }
                },
                new() {
                    delay                   = 1f,
                    PowerUpShootType        = ShootType.Interval,
                    missileData = new() {
                        missileID               = Missiles.FamiliarProjectileID,
                        SpawnPositionType       = SpawnPositionType.AtFamiliar,
                        SpawnDirection          = Directions.TowardsFamiliarDamageZone,
                        additionalMissileFlags  = MissileFlags.Explodes | MissileFlags.GrowsOnBirth,
                    },
                    
                    baseStats = new() {
                        Interval            = 0.12f,
                        Amount              = 24,
                        Cooldown            = 4f,
                    },
                    spawnOffset = new float3(0,0,-0.5f),
                }
            }
        };
    }
}