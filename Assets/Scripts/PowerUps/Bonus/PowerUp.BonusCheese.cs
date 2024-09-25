public static partial class PowerUps {
    public static PowerUp BonusCheese() {
        return new() {
            name = "Cheese",
            description = "A slice of Cheese! Heals you for 30",
            spriteName = "food",
            PowerUpEffect = PowerUpEffect.HEAL_30,
            PowerUpType = PowerUpType.Bonus,
        };
    }
}