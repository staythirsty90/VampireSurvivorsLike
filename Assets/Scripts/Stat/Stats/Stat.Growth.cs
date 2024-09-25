public static partial class Stats {
    public static System.Guid GrowthID;

    public static Stat Growth() {
        return new Stat() {
            name = "Growth",
            iconName = "GoldCrown",
        };
    }
}