public static partial class Stats {
    
    public static System.Guid CooldownReductionID;

    public static Stat CooldownReduction() {
        return new Stat() {
            name = "Cooldown Reduction",
            iconName = "CooldownReduction",
        };
    }
}