public static partial class Missiles {
    public static ID BoneID = new() { Guid = System.Guid.NewGuid() };

    static MissileArchetype Bone() => new() {
        gfx = new() {
            spriteName = "bone",
        },
        missile = new() {
            Damage = 5,
            Speed = 0.75f,
            Duration = 2f,
            Flags = 
                MissileFlags.HasTimedLife
                | MissileFlags.BouncesOffWalls
                | MissileFlags.Spins
                | MissileFlags.ShrinksOnDying
                | MissileFlags.GrowsOnBirth
                | MissileFlags.BouncesOffEnemies
                | MissileFlags.BouncesOffObstructibles
            ,
            HitType = MissileHitType.AoE_Circle2Rect,
            HitEffect = HitEffect.Normal,
            ScaleEnd = 0.8f,
            Radius = 0.2f,
            HitFrequency = 1f,
            maxHits = 1,

            StatusEffects = new() {
                new() {
                    HitEffect = HitEffect.Knockback,
                    Duration = 1.4f,
                },
            },
        }
    };
}