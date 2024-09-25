public static partial class Enemies {
    public static ID TestWolfID = new() { Guid = System.Guid.NewGuid() };

    static EnemyArchetype TestWolf() => new() {
        gfx = new() {
            spriteName = "question_mark"
        },
        enemy = new() {
            //DropsXPGems = true,
            Level = 1,
            MaxHealth = 30,
            Damage = 3,
            XPGiven = 6,
            AttackRate = 0.03f,
            MoveSpeed = 1,
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