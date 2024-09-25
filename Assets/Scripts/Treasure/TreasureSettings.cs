using Unity.Collections;

[System.Serializable]
public struct TreasureSettings {
    public FixedList32Bytes<byte> chances;
    public int level;
    public FixedList32Bytes<PrizeType> prizeTypes;

    public static TreasureSettings Create() {
        return new TreasureSettings {
            chances = new() { 0, 0, 30 },
            prizeTypes = new() { PrizeType.Evolution, PrizeType.Random, PrizeType.Random, PrizeType.Random, PrizeType.Random },
            level = 1
        };
    }
}

public enum PrizeType : byte {
    Evolution,
    ExistingWeapon,
    ExistingAny,
    NewWeapon,
    NewAny,
    Random,
}