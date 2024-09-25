public static partial class PickUps {
    public static ID GivePowerUpID = new() { Guid = System.Guid.NewGuid() };

    static PickUpArchetype GivePowerUp() => new() {
        gfx = new Gfx () {
            spriteName = "question_mark",
        },
    };
}