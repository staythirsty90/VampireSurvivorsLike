public static partial class PickUps {
    public static ID Gold1ID = new() { Guid = System.Guid.NewGuid() };

    static PickUpArchetype Gold1() => new() {
        gfx = new() {
            spriteName = "Coin",
        },
        pickUp = new() {
            value = 1,
            rarity = 50,
            spriteScale = 0.20f,
            Class = PickUpClass.Gold,
        }
    };
}