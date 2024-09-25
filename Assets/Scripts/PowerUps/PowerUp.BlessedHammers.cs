public static partial class PowerUps {
    public static PowerUp BlessedHammers() {
        return new() {
            name = "Blessed Hammers",
            maxLevel = 8,
            description = "Summon Blessed Hammers to spiral and destroy!",
            spriteName = "BlessedHammers",
            rarity = 100,
            _isWeapon = true,
            
            baseStats = new() {
                Cooldown = 3f,
                Amount = 1,
            },
            
            growthStats = new() {
                new() { Amount   = 1 },
                new() { Area     = 0.25f, Speed = 0.3f },
                new() { Duration = 0.5f,  Damage = 10 },
                new() { Amount   = 1 },
                new() { Area     = 0.25f, Speed = 0.3f },
                new() { Duration = 0.5f,  Damage = 10 },
                new() { Amount   = 1 },
            },
            
            Weapons = new() {
                new() {
                    PowerUpShootType = ShootType.BatchedMissiles,
                    missileData = new() {
                        SpawnDirection = Directions.None,
                        missileID = Missiles.BlessedHammersID,
                    }
                }
            }
        };
    }
}