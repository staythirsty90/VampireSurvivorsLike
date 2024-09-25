using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct HitEffectBuffer : IBufferElementData {
    public HitEffect HitEffect;
}

public enum HitEffect : byte {
    NONE,
    Normal,
    Freeze,
    Fire,
    Electric,
    Knockback,
}

public struct StatusEffectApplier {
    public HitEffect HitEffect;
    public float Duration;
    public float2 knockBackDirection;
}

public struct State : IComponentData {
    public bool isActive;
    public bool isDying;
    public float timeOfDeath;
    public int frameOfDeath;
    public float timeAlive;
    public bool readyToActivate;
    public bool needsSpriteAndParticleSystem;
    public FixedList128Bytes<StatusEffectApplier> StatusEffects;
    public bool doRecycle;
    public ushort RecycleCount;
    public sbyte minuteOfSpawn;
    public byte numberOfSpawnsThisWave;
    public float angle;

    public readonly bool IsActiveAndNotDying() {
        return isActive && !isDying;
    }

    public readonly bool WillBeActivated(bool needsRendererUpdated) {
        return isActive || readyToActivate || needsRendererUpdated;
    }
    public void Reset() {
        
        isActive                        = false;
        isDying                         = false;
        timeOfDeath                     = 0;
        timeAlive                       = 0;
        frameOfDeath                    = 0;
        angle                           = 0;
        readyToActivate                 = false;
        needsSpriteAndParticleSystem    = false;
        doRecycle                       = false;
        
        RecycleCount                    += 1;

        for(int i = 0; i < StatusEffects.Length; i++) {
            var effect = StatusEffects[i];
            effect.Duration = 0;
            effect.knockBackDirection = float2.zero;
            StatusEffects[i] = effect;
        }
    }
}