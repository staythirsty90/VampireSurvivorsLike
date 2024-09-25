public static partial class Stats {
    public static System.Guid HealthRegenerationID;

    public static Stat HealthRegeneration() {
        return new Stat() {
            name = "Health Regeneration",
            iconName = "HealthRegen",
        };
    }
}