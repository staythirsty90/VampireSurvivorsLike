using Unity.Collections;

public enum TalentDescType {
    Player,
    Missile,
}

[System.Serializable]
public struct Talent {
    public StatIncrease StatIncrease;
    public FixedString64Bytes Icon;
    public FixedString64Bytes Name;
    public FixedString128Bytes Desc;
    public uint Cost;
    public byte CurrentRank;
    public byte MaximumRank;
    public uint CostPerRank;
    public TalentDescType TalentDescType;

    public static void UpdateDesc(ref Talent t) {
        var value = t.StatIncrease.isPercentageBased ? t.StatIncrease.value.ToString() + '%' : t.StatIncrease.value.ToString();
        switch(t.TalentDescType) {
            case TalentDescType.Player:
                t.Desc = $"Increase your {t.Name} by {value} per Rank";
                break;
            case TalentDescType.Missile:
                t.Desc = $"Increase the {t.Name} of your Missiles by {value} per Rank";
                break;
        }
    }

    public static bool RankUp(ref Talent t) {
        if(t.CurrentRank >= t.MaximumRank) {
            return false;
        }
        t.CurrentRank += 1;
        t.Cost = t.CostPerRank + (t.CurrentRank * t.CostPerRank);
        UpdateDesc(ref t);
        return true;
    }
}