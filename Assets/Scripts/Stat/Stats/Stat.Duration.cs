public static partial class Stats {
    public static System.Guid DurationID;

    public static Stat Duration() {
        return new Stat() {
            name = "Duration",
            iconName = "Duration",
        };
    }
}