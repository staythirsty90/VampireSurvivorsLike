using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public partial class ParticleSystemHelper : SystemBase {

    static readonly Dictionary<Guid, ParticleSystem> dictionary = new ();

    protected override void OnUpdate() {}

    void Load(in ID psID, string name) {
        var ps = Resources.Load<ParticleSystem>($"ParticleSystems/Particle System {name}");
        Debug.Assert(ps);
        dictionary.Add(psID.Guid, ps);
    }

    protected override void OnStartRunning() {
        base.OnCreate();
        
        dictionary.Clear();

        Load(Missiles.ImmolationID,             "Immolation");
        Load(Missiles.FrostboltID,              "Frost");
        Load(Missiles.FrozenOrbFrostboltID,     "Frost");
        Load(Missiles.FrozenOrbID,              "FrozenOrb");
        Load(Missiles.AxeID,                    "Axe");
        Load(Missiles.DivineShieldID,           "Divine");
        Load(Missiles.MeteorID,                 "Meteor");
        Load(Missiles.RunetracerID,             "Rune");
        Load(Missiles.MagicMissileID,           "Magic");
        Load(Missiles.ClockStopID,              "Clock");
        Load(Missiles.BoomerangID,              "Boomerang");
        Load(Missiles.BlessedHammersID,         "Blessed");
        Load(Missiles.FireFieldID,              "Fire Field");
        Load(Missiles.SwordID,                  "Sword");
        Load(Missiles.ExplosionID,              "Explosion");
        Load(Missiles.FamiliarExplosionID,      "FamiliarExplosion");
        Load(Missiles.FamiliarDamageZoneID,     "DamageZone");
        Load(Missiles.FamiliarProjectileID,     "Projectile");
        Load(Missiles.LightningID,              "Lightning");
        
        Load(PickUps.RosaryID,                  "Rosary");
        Load(PickUps.Heal30ID,                  "Food");
        Load(PickUps.MagnetID,                  "Magnet");
        Load(PickUps.CloverID,                  "Clover");

        Load(Enemies.TorchID,                   "Brazier");
        Load(Enemies.CandelabraID,              "Candelabra");
    }
    
    //
    // NOTE: For love of God is there a better way to copy a ParticleSystem without having to manually copy
    // each of its modules and fields one at a time??
    //
    public static void SetParticleSystem(Guid missileGuid, ParticleSystem pslink) {
        if(!dictionary.TryGetValue(missileGuid, out ParticleSystem sourcePS)) { 
            pslink.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            //Debug.Log($"Stopping particle system because source missile doesn't have one, returning!, {linkid}");
            return;
        }
    
        var sourceTexture = sourcePS.textureSheetAnimation;

        if(sourceTexture.enabled) {
            var targetTexture = pslink.textureSheetAnimation;
            var og_count = targetTexture.spriteCount;
            for(int i = og_count - 1; i > -1; i--) {
                //Debug.Log($"targetTexure sprite name_{i}: {targetTexture.GetSprite(i)}");
                targetTexture.RemoveSprite(i);
            }
            Debug.Assert(targetTexture.spriteCount == 0);

            for(int i = 0; i < sourceTexture.spriteCount; i++) {
                //Debug.Log($"sourceTexure sprite name_{i}: {sourceTexture.GetSprite(i)}");
                var sprite = sourceTexture.GetSprite(i);
                if(sprite != null) {
                    targetTexture.AddSprite(sprite);
                }
            }

            targetTexture.cycleCount            = sourceTexture.cycleCount;
            targetTexture.timeMode              = sourceTexture.timeMode;
            targetTexture.startFrame            = sourceTexture.startFrame;
            targetTexture.fps                   = sourceTexture.fps;
            targetTexture.enabled               = true;
        }
    
        pslink.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        var targetShape                         = pslink.shape;
        targetShape.radius                      = sourcePS.shape.radius;
        targetShape.radiusThickness             = sourcePS.shape.radiusThickness;
        targetShape.radiusMode                  = sourcePS.shape.radiusMode;
        targetShape.shapeType                   = sourcePS.shape.shapeType;
        targetShape.arc                         = sourcePS.shape.arc;
        targetShape.arcSpread                   = sourcePS.shape.arcSpread;
        targetShape.angle                       = sourcePS.shape.angle;
        targetShape.arcMode                     = sourcePS.shape.arcMode;
        targetShape.arcSpeed                    = sourcePS.shape.arcSpeed;
        targetShape.arcSpeedMultiplier          = sourcePS.shape.arcSpeedMultiplier;
        targetShape.alignToDirection            = sourcePS.shape.alignToDirection;
        targetShape.randomDirectionAmount       = sourcePS.shape.randomDirectionAmount;
        targetShape.sphericalDirectionAmount    = sourcePS.shape.sphericalDirectionAmount;
        targetShape.radiusSpeed                 = sourcePS.shape.radiusSpeed;
        targetShape.randomPositionAmount        = sourcePS.shape.randomPositionAmount;
        targetShape.rotation                    = sourcePS.shape.rotation;
        targetShape.scale                       = sourcePS.shape.scale;
        targetShape.position                    = sourcePS.shape.position;
        targetShape.enabled                     = sourcePS.shape.enabled;

        var main                                = pslink.main;
        main.startColor                         = sourcePS.main.startColor;
        main.startSize                          = sourcePS.main.startSize;
        main.simulationSpace                    = sourcePS.main.simulationSpace;
        main.simulationSpeed                    = sourcePS.main.simulationSpeed;
        main.gravityModifier                    = sourcePS.main.gravityModifier;
        main.duration                           = sourcePS.main.duration;
        main.startLifetime                      = sourcePS.main.startLifetime;
        main.startSpeed                         = sourcePS.main.startSpeed;
        main.startSpeedMultiplier               = sourcePS.main.startSpeedMultiplier;
        main.loop                               = sourcePS.main.loop;
        main.prewarm                            = sourcePS.main.prewarm;
        main.cullingMode                        = sourcePS.main.cullingMode;
        main.scalingMode                        = sourcePS.main.scalingMode;
        main.startRotation                      = sourcePS.main.startRotation;
        main.startRotation3D                    = sourcePS.main.startRotation3D;
        main.startRotationMultiplier            = sourcePS.main.startRotationMultiplier;
        main.startRotationX                     = sourcePS.main.startRotationX;
        main.startRotationY                     = sourcePS.main.startRotationY;
        main.startRotationZ                     = sourcePS.main.startRotationZ;
        main.maxParticles                       = sourcePS.main.maxParticles;

        var velOverLT                           = pslink.velocityOverLifetime;
        velOverLT.x                             = sourcePS.velocityOverLifetime.x;
        velOverLT.y                             = sourcePS.velocityOverLifetime.y;
        velOverLT.z                             = sourcePS.velocityOverLifetime.z;
        velOverLT.space                         = sourcePS.velocityOverLifetime.space;
        velOverLT.orbitalX                      = sourcePS.velocityOverLifetime.orbitalX;
        velOverLT.orbitalY                      = sourcePS.velocityOverLifetime.orbitalY;
        velOverLT.orbitalZ                      = sourcePS.velocityOverLifetime.orbitalZ;
        velOverLT.radial                        = sourcePS.velocityOverLifetime.radial;
        velOverLT.radialMultiplier              = sourcePS.velocityOverLifetime.radialMultiplier;
        velOverLT.enabled                       = sourcePS.velocityOverLifetime.enabled;

        var colorOverLT                         = pslink.colorOverLifetime;
        colorOverLT.color                       = sourcePS.colorOverLifetime.color;
        colorOverLT.enabled                     = sourcePS.colorOverLifetime.enabled;

        var colorBySpeed                        = pslink.colorBySpeed;
        colorBySpeed.color                      = sourcePS.colorBySpeed.color;
        colorBySpeed.range                      = sourcePS.colorBySpeed.range;
        colorBySpeed.enabled                    = sourcePS.colorBySpeed.enabled;

        var sizeOverLT                          = pslink.sizeOverLifetime;
        sizeOverLT.size                         = sourcePS.sizeOverLifetime.size;
        sizeOverLT.sizeMultiplier               = sourcePS.sizeOverLifetime.sizeMultiplier;
        sizeOverLT.xMultiplier                  = sourcePS.sizeOverLifetime.xMultiplier;
        sizeOverLT.separateAxes                 = sourcePS.sizeOverLifetime.separateAxes;
        sizeOverLT.yMultiplier                  = sourcePS.sizeOverLifetime.yMultiplier;
        sizeOverLT.zMultiplier                  = sourcePS.sizeOverLifetime.zMultiplier;
        sizeOverLT.x                            = sourcePS.sizeOverLifetime.x;
        sizeOverLT.y                            = sourcePS.sizeOverLifetime.y;
        sizeOverLT.z                            = sourcePS.sizeOverLifetime.z;
        sizeOverLT.enabled                      = sourcePS.sizeOverLifetime.enabled;

        var emission                            = pslink.emission;
        var emissionROT                         = pslink.emission.rateOverTime;

        emissionROT.curve                       = sourcePS.emission.rateOverTime.curve;
        emissionROT.curveMultiplier             = sourcePS.emission.rateOverTime.curveMultiplier;
        emissionROT.curveMin                    = sourcePS.emission.rateOverTime.curveMin;
        emissionROT.curveMax                    = sourcePS.emission.rateOverTime.curveMax;
        emissionROT.constant                    = sourcePS.emission.rateOverTime.constant;
        emissionROT.constantMin                 = sourcePS.emission.rateOverTime.constantMin;
        emissionROT.constantMax                 = sourcePS.emission.rateOverTime.constantMax;
        emission.rateOverTimeMultiplier         = sourcePS.emission.rateOverTimeMultiplier;

        if(sourcePS.emission.burstCount > 0) {
            emission.burstCount = sourcePS.emission.burstCount;
            emission.SetBurst(0, sourcePS.emission.GetBurst(0));
        }
        else {
            emission.burstCount = 0;
        }
        emission.rateOverTime                   = emissionROT;
        emission                                = sourcePS.emission;
        emission.enabled                        = sourcePS.emission.enabled;

        var rotation                            = pslink.rotationOverLifetime;
        rotation.x                              = sourcePS.rotationOverLifetime.x;
        rotation.y                              = sourcePS.rotationOverLifetime.y;
        rotation.z                              = sourcePS.rotationOverLifetime.z;
        rotation.enabled                        = sourcePS.rotationOverLifetime.enabled;

        var forceOverLT                         = pslink.forceOverLifetime;
        forceOverLT.x                           = sourcePS.forceOverLifetime.x;
        forceOverLT.y                           = sourcePS.forceOverLifetime.y;
        forceOverLT.z                           = sourcePS.forceOverLifetime.z;
        forceOverLT.space                       = sourcePS.forceOverLifetime.space;
        forceOverLT.randomized                  = sourcePS.forceOverLifetime.randomized;
        forceOverLT.xMultiplier                 = sourcePS.forceOverLifetime.xMultiplier;
        forceOverLT.yMultiplier                 = sourcePS.forceOverLifetime.yMultiplier;
        forceOverLT.zMultiplier                 = sourcePS.forceOverLifetime.zMultiplier;
        forceOverLT.enabled                     = sourcePS.forceOverLifetime.enabled;

        var noise = pslink.noise;
        noise.enabled                           = sourcePS.noise.enabled;
        noise.strength                          = sourcePS.noise.strength;
        noise.frequency                         = sourcePS.noise.frequency;
        noise.scrollSpeed                       = sourcePS.noise.scrollSpeed;
        noise.damping                           = sourcePS.noise.damping;
        noise.octaveCount                       = sourcePS.noise.octaveCount;
        noise.octaveMultiplier                  = sourcePS.noise.octaveMultiplier;
        noise.octaveScale                       = sourcePS.noise.octaveScale;
        noise.quality                           = sourcePS.noise.quality;
        noise.positionAmount                    = sourcePS.noise.positionAmount;
        noise.rotationAmount                    = sourcePS.noise.rotationAmount;
        noise.sizeAmount                        = sourcePS.noise.sizeAmount;
        noise.enabled                           = sourcePS.noise.enabled;

        var targetRenderer                      = pslink.GetComponent<ParticleSystemRenderer>();
        var sourceRenderer                      = sourcePS.GetComponent<ParticleSystemRenderer>();
        targetRenderer.sortingLayerName         = sourceRenderer.sortingLayerName;
        targetRenderer.sortingLayerID           = sourceRenderer.sortingLayerID;
        targetRenderer.sortingOrder             = sourceRenderer.sortingOrder;
        targetRenderer.renderMode               = sourceRenderer.renderMode;
        targetRenderer.alignment                = sourceRenderer.alignment;
        targetRenderer.cameraVelocityScale      = sourceRenderer.cameraVelocityScale;
        targetRenderer.velocityScale            = sourceRenderer.velocityScale;
        targetRenderer.lengthScale              = sourceRenderer.lengthScale;
        targetRenderer.enabled                  = sourceRenderer.enabled;
        
        pslink.Play();
        pslink.Emit(1);
    }
}