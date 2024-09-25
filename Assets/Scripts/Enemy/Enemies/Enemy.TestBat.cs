public static partial class Enemies {
    public static ID TestBatID = new() { Guid = System.Guid.NewGuid() };

    static EnemyArchetype TestBat() => new() {
        gfx = new() {
            spriteName = "bat",
        },
        enemy = new() {
            Level = 1,
            MaxHealth = 5,
            Damage = 1,
            XPGiven = 1,
            AttackRate = 0.3f,
            MoveSpeed = 1,
            AttackRange = 1,
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