public static partial class Stats {

    public static System.Guid InvulnerableID;

    public static Stat Invulnerable() {
        return new Stat() {
            name = "Invulnerable",
            iconName = "icon_itemicon_gold",
            invisible = true,
            skipRecalc = true,
        };
    }
}