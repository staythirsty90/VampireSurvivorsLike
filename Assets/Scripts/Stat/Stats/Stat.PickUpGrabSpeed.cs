public static partial class Stats {
    public static System.Guid PickUpGrabSpeedID;

    public static Stat PickUpSpeed() {
        return new Stat() {
            name = "PickUp Grab Speed",
            iconName = "Range",
        };
    }
}