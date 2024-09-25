public static partial class Characters {
    public static Character Knight() {
        var ch = new Character {
            charName = "Knight",
            startingWeapons = new() {
                //PowerUps.FrostboltEvolved().name,
                //PowerUps.Familiar().name,
                //PowerUps.Bone().name,
                //PowerUps.MagicWand().name,
                PowerUps.ClockStop().name,
                //PowerUps.Immolation().name,
                //PowerUps.Boomerang().name,
                PowerUps.Axe().name,
                //PowerUps.Whip().name,
                //PowerUps.Meteor().name,
                //PowerUps.Frostbolt().name,
                //PowerUps.Speed().name,
                //PowerUps.Shield().name,
                //PowerUps.BlessedHammers().name,
                //PowerUps.Thunderstorm().name,
                //PowerUps.Runetracer().name,
                //PowerUps.Sword().name,

                //PowerUps.Armor().name,
            },

            spriteScale = 0.45f,

            idleSpriteNames = new() {
                "knight",
            },

            runSpriteNames = new() {
                "knight",
            },
        };

        ch.SetStat(Stats.MaxHealthID, 223123);
        ch.SetStat(Stats.MovementSpeedID, 5.5f);
        ch.SetStat(Stats.DurationID, 5.5f);
        ch.SetStat(Stats.LuckID, 1f);
        ch.SetStat(Stats.PickUpGrabSpeedID, 3);
        ch.SetStat(Stats.PickUpGrabRangeID, 3);
        ch.SetStat(Stats.AmountID, 5);
        ch.SetStat(Stats.CooldownReductionID, 0.67f);

        ch.desc = Character.GenerateStatDescription(ch.CharacterStats.statIncreaseOnLevel);
        return ch;
    }
}