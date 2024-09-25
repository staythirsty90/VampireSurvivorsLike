public static partial class PowerUps {
    public static PowerUp Axe() {
        return new() {
            name = "Axe",
            maxLevel = 8,
            description = "Summon Axe to slash through enemies!",
            spriteName = "AxePowerUp",
            rarity = 100,
            _isWeapon = true,
            baseStats = new() {
                Cooldown = 4f,
                Amount = 1,
                Interval = 0.2f,
            },
            growthStats = new() {
                new() { Cooldown = -0.2f }, // Level 2
                new() { Cooldown = -0.2f }, // Level 3
                new() { Cooldown = -0.2f }, // Level 4
                new() { Cooldown = -0.2f }, // Level 5
                new() { Cooldown = -0.2f }, // Level 6
                new() { Cooldown = -0.2f }, // Level 7
                new() { Cooldown = -0.2f }, // Level 8
            },

            Weapons = new() {
                new() {
                    missileData = new() {
                        missileID = Missiles.AxeID,
                    },
                    PowerUpShootType = ShootType.Interval,
                }
            },
        };
    }
}