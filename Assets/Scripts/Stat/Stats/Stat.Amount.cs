public static partial class Stats {

    public static System.Guid AmountID;

    public static Stat Amount() {
        return new Stat() {
            name = "Amount",
            iconName = "SoJ",
        };
    }
}