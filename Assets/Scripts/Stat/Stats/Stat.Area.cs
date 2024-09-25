public static partial class Stats {
    public static System.Guid AreaID;

    public static Stat Area() {
        return new Stat() {
            name = "Area",
            iconName = "Area",
        };
    }
}