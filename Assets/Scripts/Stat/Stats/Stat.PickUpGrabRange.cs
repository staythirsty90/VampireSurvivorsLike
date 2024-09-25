public static partial class Stats {
    public static System.Guid PickUpGrabRangeID;

    public static Stat PickUpRange() {
        return new Stat() {
            name = "PickUp Grab Range",
            iconName = "Range",
        };
    }
}