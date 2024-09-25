public static partial class PowerUps {
    public static PowerUp Meteor() {
        return new PowerUp() {
            name = "Meteor",
            maxLevel = 8,
            description = "Summon a Meteor from the sky!",
            spriteName = "Meteor",
            rarity = 100,
            _isWeapon = true,

            baseStats = new() {
                Cooldown = 5f,
                Amount = 1,
                Interval = 0.5f,
            },

            growthStats = new() {
                new() { Cooldown = -0.2f },
                new() { Amount = 1, Area = 0.2f },
                new() { Duration = 0.5f, Damage = 10 },
                new() { Amount = 1, Area = 0.2f },
                new() { Duration = 0.3f, Damage = 10 },
                new() { Amount = 1, Area = 0.2f },
                new() { Duration = 0.3f, Damage = 5 },
                new() { Area = 0.2f, Damage = 5 },
            },

            Weapons = new () {
                new() {
                    PowerUpShootType = ShootType.Interval,
                    missileData = new() {
                        SpawnDirection = Directions.NearestEnemy_Then_Clockwise,
                        missileID = Missiles.MeteorID,
                    }
                }
            }
        };
    }
}