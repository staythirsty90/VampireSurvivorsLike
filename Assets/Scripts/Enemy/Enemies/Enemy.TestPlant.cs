public static partial class Enemies {
    public static ID TestPlantID = new() { Guid = System.Guid.NewGuid() };

    static EnemyArchetype TestPlant() => new() {
        gfx = new() {
            spriteName = "blob"
        },
        enemy = new() {
            Level = 1,
            MaxHealth = 30,
            Damage = 1,
            XPGiven = 1,
            AttackRate = 0.1f,
            MoveSpeed = 1,
        },
        spriteScale = new() {
            Value = 0.3f,
        },
        movement = new() {
            MoveSpeed = 1,
            MoveType = MoveType.Forward,
        },
        drops = new() {
            DropsXPGems = true,
        }
    };
}