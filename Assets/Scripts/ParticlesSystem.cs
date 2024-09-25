using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.ParticleSystemJobs;
using Unity.Burst;
using Unity.Collections;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class ParticlesSystem : SystemBase {

    ParticleSystem ps_normal;
    ParticleSystem ps_freeze;
    ParticleSystem ps_fire;
    ParticleSystem ps_elec;

    Transform ps_normalTF;
    Transform ps_freezeTF;
    Transform ps_fireTF;
    Transform ps_elecTF;

    ParticleSystem.Particle[] parts_normal;
    ParticleSystem.Particle[] parts_fire;
    ParticleSystem.Particle[] parts_freeze;
    ParticleSystem.Particle[] parts_elec;

    protected override void OnCreate() {
        base.OnCreate();
        ps_normalTF = Object.Instantiate(Resources.Load<Transform>("ParticleSystems/Particle System Normal").transform, new Vector3(-500, 0, 0), Quaternion.identity);
        ps_fireTF   = Object.Instantiate(Resources.Load<Transform>("ParticleSystems/Particle System Fire").transform, new Vector3(-500,0,0), Quaternion.identity);
        ps_freezeTF = Object.Instantiate(Resources.Load<Transform>("ParticleSystems/Particle System Freeze").transform, new Vector3(-500,0,0), Quaternion.identity);
        ps_elecTF   = Object.Instantiate(Resources.Load<Transform>("ParticleSystems/Particle System Electric").transform, new Vector3(-500,0,0), Quaternion.identity);

        ps_normal   = ps_normalTF.GetComponent<ParticleSystem>();
        ps_fire     = ps_fireTF.GetComponent<ParticleSystem>();
        ps_freeze   = ps_freezeTF.GetComponent<ParticleSystem>();
        ps_elec     = ps_elecTF.GetComponent<ParticleSystem>();

        Debug.Assert(ps_normal != null);
        Debug.Assert(ps_fire != null);
        Debug.Assert(ps_freeze != null);
        Debug.Assert(ps_elec != null);

        parts_normal = new ParticleSystem.Particle[ps_normal.main.maxParticles];
        parts_fire   = new ParticleSystem.Particle[ps_fire.main.maxParticles];
        parts_freeze = new ParticleSystem.Particle[ps_freeze.main.maxParticles];
        parts_elec   = new ParticleSystem.Particle[ps_elec.main.maxParticles];
    }

    protected override void OnUpdate() {
        var dt = UnityEngine.Time.deltaTime;
        var playerMovement = SystemAPI.GetSingleton<FrameStatsSingleton>().playerMoveDelta;
        var hitEffects = new NativeList<HitEffect>(Allocator.TempJob);
        var vectors = new NativeList<float3>(Allocator.TempJob);

        Entities
            .WithAll<Enemy>()
            .ForEach((ref DynamicBuffer<HitEffectBuffer> hitbuffer, in BoundingBox bb, in State state) => {
                if(!state.isActive) return;

                var length = hitbuffer.Length;
                for(int i = 0; i < length; i++) {
                    var hit = hitbuffer[i];
                    if(hit.HitEffect == HitEffect.NONE) {
                        return;
                    }
                    hitEffects.Add(hit.HitEffect);
                    vectors.Add(bb.center);
                    hit.HitEffect = HitEffect.NONE;
                    hitbuffer[i] = hit;
                }
            }).Run();

        var length = hitEffects.Length;
        for(var i = 0; i < length; i++) {
            var he = hitEffects[i];
            var pos = vectors[i];
            switch(he) {
                case HitEffect.Normal:
                    ps_normalTF.position = pos;
                    ps_normal.Emit(1);
                    break;
                case HitEffect.Freeze:
                    ps_freezeTF.position = pos;
                    ps_freeze.Emit(1);
                    break;
                case HitEffect.Fire:
                    ps_fireTF.position = pos;
                    ps_fire.Emit(1);
                    break;
                case HitEffect.Electric:
                    ps_elecTF.position = pos;
                    ps_elec.Emit(1);
                    break;
                case HitEffect.Knockback:
                    break;
            }
        }

        hitEffects.Dispose();
        vectors.Dispose();

        // GetParticles is allocation free because we reuse the Particles buffer between updates
        int numParticlesAlive = ps_normal.GetParticles(parts_normal);
        for(int i = 0; i < numParticlesAlive; i++) {
            parts_normal[i].position -= (Vector3)playerMovement;
        }
        ps_normal.SetParticles(parts_normal, numParticlesAlive);

        numParticlesAlive = ps_fire.GetParticles(parts_fire);
        for(int i = 0; i < numParticlesAlive; i++) {
            parts_fire[i].position -= (Vector3)playerMovement;
        }
        ps_fire.SetParticles(parts_fire, numParticlesAlive);

        numParticlesAlive = ps_freeze.GetParticles(parts_freeze);
        for(int i = 0; i < numParticlesAlive; i++) {
            parts_freeze[i].position -= (Vector3)playerMovement;
        }
        ps_freeze.SetParticles(parts_freeze, numParticlesAlive);

        numParticlesAlive = ps_elec.GetParticles(parts_elec);
        for(int i = 0; i < numParticlesAlive; i++) {
            parts_elec[i].position -= (Vector3)playerMovement;
        }
        ps_elec.SetParticles(parts_elec, numParticlesAlive);

        ps_normalTF.position    = new Vector3(-500, -500, 0);
        ps_fireTF.position      = new Vector3(-500, -500, 0);
        ps_freezeTF.position    = new Vector3(-500, -500, 0);
        ps_elecTF.position      = new Vector3(-500, -500, 0);

        Entities.ForEach((Entity e, ParticleSystem ps, in OffsetMovement m, in State state) => {
            if(!state.isActive) return;
            if((m.Flags & OffsetFlags.NoParticlesOffset) != 0) return;
            
            new UpdateParticlesJob {
                playerMovement = playerMovement,
            }.ScheduleBatch(ps, 2048);

        }).WithoutBurst().Run();
    }
}

[BurstCompile]
public struct UpdateParticlesJob : IJobParticleSystemParallelForBatch {
    [ReadOnly]
    public Vector3 playerMovement;

    public void Execute(ParticleSystemJobData particles, int startIndex, int count) {
        //Debug.Log($"update particles job");
        if(particles.count == 0) return;
        var positionsX = particles.positions.x;
        var positionsY = particles.positions.y;
        int endIndex = startIndex + count;
        for(int i = startIndex; i < endIndex; i++) {
            positionsX[i] -= playerMovement.x;
            positionsY[i] -= playerMovement.y;
        }
    }
}