public static partial class PickUps {
    public static ID RosaryID = new() { Guid = System.Guid.NewGuid() };

    static PickUpArchetype Rosary() => new() {
        gfx = new() {
            spriteName = "rosary"
        },
        pickUp = new() {
            rarity = 20,
            spriteScale = 0.25f,
            Class = PickUpClass.Rosary
        }
    };
}