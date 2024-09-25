public static partial class Stats {
    public static System.Guid MovementSpeedID;

    public static Stat MovementSpeed() {
        return new Stat() {
            name = "Movement Speed",
            iconName = "MoveSpeed",
        };
    }
}