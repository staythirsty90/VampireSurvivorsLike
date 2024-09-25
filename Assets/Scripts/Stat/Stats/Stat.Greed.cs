public static partial class Stats {
    public static System.Guid GreedID;

    public static Stat Greed() {
        return new Stat() {
            name = "Greed",
            iconName = "GoldMask",
        };
    }
}