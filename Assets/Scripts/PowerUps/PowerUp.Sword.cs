public static partial class PowerUps {
    public static PowerUp Sword() {
        return new() {
            name = "Sword",
            maxLevel = 8,
            description = "Fires a Sword towards players direction.",
            spriteName = "swordPowerUp",
            rarity = 100,
            _isWeapon = true,

            baseStats = new() {
                Cooldown = 1f,
                Amount = 1,
                Interval = 0.1f,
            },

            growthStats = new() {
                new() { Amount   = 1 },
                new() { Amount   = 1, Damage = 5 },
                new() { Amount   = 1 },
                new() { Piercing = 1 },
                new() { Amount   = 1 },
                new() { Amount   = 1, Damage = 5 },
                new() { Piercing = 1 },
            },

            Weapons = new() {
                new() {
                    PowerUpShootType = ShootType.Interval,
                    missileData = new() {
                        missileID = Missiles.SwordID,
                        SpawnDirection = Directions.PlayersLastMovement,
                        SpawnVariationType = SpawnVariationType.RandomSpreadWithArea_Knife,
                    }
                }
            },
        };
    }
}