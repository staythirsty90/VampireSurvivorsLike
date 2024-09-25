public static partial class Stats {
    public static System.Guid ArmorID;

    public static Stat Armor() {
        return new Stat() {
            name = "Armor",
            iconName = "Armor",
        };
    }
}