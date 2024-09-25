public static partial class Stats {
    public static System.Guid SpeedID;

    public static Stat Speed() {
        return new Stat() {
            name = "Speed",
            iconName = "Speed",
        };
    }
}