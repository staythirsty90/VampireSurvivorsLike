public static partial class Stats {
    public static System.Guid HealthID;

    public static Stat Health() {
        return new Stat() {
            name = "Health",
            iconName = "MaxHealth",
            skipRecalc = true,
        };
    }
}