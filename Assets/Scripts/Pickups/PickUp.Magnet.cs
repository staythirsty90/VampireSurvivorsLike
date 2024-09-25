public static partial class PickUps {
    public static ID MagnetID = new() { Guid = System.Guid.NewGuid() };

    static PickUpArchetype Magnet() => new() {
        gfx = new() {
            spriteName = "none"
        },
        pickUp = new() {

            rarity = 2,
            unlocksAt = 12,
            spriteScale = 2,
            Class = PickUpClass.Magnet
        }
    };
}