public static partial class Enemies {
    public static ID TestSwarmID = new() { Guid = System.Guid.NewGuid() };

    static EnemyArchetype TestSwarm() => new() {
        gfx = new() {
            spriteName = "bat"
        },
        enemy = new() {
            Level = 1,
            MaxHealth = 5,
            Damage = 1,
            XPGiven = 1,
            AttackRate = 1f,
            MoveSpeed = 7,
            specType = Enemy.SpecType.Swarmer,
        },
        movement = new() {
            MoveSpeed = 4,
            MoveType = MoveType.Forward,
        },
        drops = new() {
            DropsXPGems = true,
        }
    };
}