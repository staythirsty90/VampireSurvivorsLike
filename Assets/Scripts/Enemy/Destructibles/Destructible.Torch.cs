public static partial class Enemies {

    public static ID TorchID = new() { Guid = System.Guid.NewGuid() };

    static EnemyArchetype Torch() => new() {
        gfx = new() {
            spriteName = "brazier",
        },
        offsetMovement = new() {
            Flags = OffsetFlags.NoParticlesOffset,
        },
        enemy = new() {
            //DropsPickups = true,
            MaxHealth = 1,
            Flags = EnemyFlags.IgnoreKillCount,
        },
        drops = new() {
            DropsPickups = true,
        }
    };
}