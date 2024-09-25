public static partial class Enemies {

    public static ID CandelabraID = new() { Guid = System.Guid.NewGuid() };

    static EnemyArchetype Candelabra() => new() {
        gfx = new() {
            spriteName = "candelabra",
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