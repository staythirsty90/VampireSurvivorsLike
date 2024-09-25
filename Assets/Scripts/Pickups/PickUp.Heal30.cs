public static partial class PickUps {
    public static ID Heal30ID = new() { Guid = System.Guid.NewGuid() };

    static PickUpArchetype Heal30() => new() {
        gfx = new() {
            spriteName = "food",
        },
        pickUp = new() {
            value = 30,
            rarity = 12,
            usesLuck = true,
            spriteScale = 0.2f,
            Class = PickUpClass.Heal,
        }
    };
}