public static partial class Enemies {
    public static ID TestBossID = new() { Guid = System.Guid.NewGuid() };

    static EnemyArchetype TestBoss() => new() {
        gfx = new() {
            spriteName = "bat_elite",
        },
        enemy = new() {
            specType = Enemy.SpecType.Elite,
            Level = 1,
            MaxHealth = 30,
            Damage = 1,
            XPGiven = 50,
            AttackRate = 0.3f,
            MoveSpeed = 5,
            AttackRange = 1,
        },
        movement = new() {
            MoveSpeed = 2,
            MoveType = MoveType.Forward,
        },
        drops = new() {
            DropsXPGems = true,
            DropsTreasure = true,
        }
    };
}