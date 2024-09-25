public static partial class Stats {
    public static System.Guid LuckID;

    public static Stat Luck() {
        return new Stat() {
            name = "Luck",
            iconName = "Clover",
        };
    }
}