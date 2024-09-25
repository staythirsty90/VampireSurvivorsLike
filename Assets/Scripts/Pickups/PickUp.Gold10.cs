public static partial class PickUps {
    public static ID Gold10ID = new() { Guid = System.Guid.NewGuid() };

    static PickUpArchetype Gold10() => new() {
        gfx = new() {
            spriteName = "coinbag",
        },
        pickUp = new() {
            value = 10,
            rarity = 50,
            spriteScale = 0.30f,
            Class = PickUpClass.Gold,
        }
    };
}