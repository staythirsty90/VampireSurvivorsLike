public static partial class Stats {
    public static System.Guid MaxHealthID;

    public static Stat MaxHealth() {
        return new Stat() {
            name = "MaxHealth",
            iconName = "MaxHealth",
            invisible = true,
        };
    }
}