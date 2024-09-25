public static partial class Enemies {

    public static ID SkeletonID = new() { Guid = System.Guid.NewGuid() };

    static EnemyArchetype Skeleton() => new() {
        gfx = new() {
            spriteName = "skeleton",
        },
        enemy = new() {
            Level = 1,
            MaxHealth = 50000,
            Damage = 2,
            XPGiven = 12,
            AttackRate = 0.23f,
            MoveSpeed = 0,
            AttackRange = 1,
        },
        movement = new() {
            MoveType = MoveType.Forward,
            MoveSpeed = 1,
        },
        drops = new() {
            DropsXPGems = true,
        },
    };
}