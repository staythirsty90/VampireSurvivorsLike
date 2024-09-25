public static partial class PickUps {
    public static ID XpGemID = new() { Guid = System.Guid.NewGuid() };

    static PickUpArchetype XpGem() => new() {
        gfx = new() {
            spriteName = "bluegem",
        },
        pickUp = new() {
            value = 1,
            rarity = 50
        }
    };
}