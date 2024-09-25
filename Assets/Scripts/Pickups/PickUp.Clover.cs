public static partial class PickUps {
    public static ID CloverID = new() { Guid = System.Guid.NewGuid() };

    static PickUpArchetype Clover() => new() {
        gfx = new() {
            spriteName = "Clover_w"
        },
        pickUp = new() {
            rarity = 20,
            spriteScale = 0.35f,
            StatIncrease = new StatIncrease() {
                statType = Stats.LuckID,
                value = 1,
            },
        },
    };
}