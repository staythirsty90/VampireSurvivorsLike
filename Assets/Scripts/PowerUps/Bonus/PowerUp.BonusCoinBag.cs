public static partial class PowerUps {
    public static PowerUp BonusCoinBag() {
        return new() {
            name = "Coin Bag",
            description = "A bag of gold! Gives +50 Coins",
            spriteName = "coinbag",
            PowerUpEffect = PowerUpEffect.COIN_50,
            PowerUpType = PowerUpType.Bonus,
        };
    }
}