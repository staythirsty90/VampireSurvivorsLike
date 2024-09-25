using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class AttractSystem : SystemBase {
    protected override void OnUpdate() {
        var center = float3.zero;
        var dt = UnityEngine.Time.deltaTime;
        var config_ememyspeed = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<StageManager>().CurrentStage.configuration.EnemySpeed;

        Entities
            .WithNone<Destructible>()
            .ForEach((ref LocalTransform ltf, ref Movement movement, in Enemy enemy, in State state) => {
                if(!state.isActive) return;
                if(state.isDying) return;
                if(Utils.IsLocked(state.StatusEffects)) return;
                if(enemy.specType == Enemy.SpecType.Swarmer) return;
                var dir = center - ltf.Position;

                var m = movement.MoveSpeed * config_ememyspeed;
                var d = math.normalize(dir) * m * dt;
                movement.Direction = d;
                //Debug.Log($"d:{d}");

                ltf.Position += d;
                ltf.Position.z = 0; // NOTE: set z to 0 because the Physics system may nudge enemies z positions, which we don't want!
            }).ScheduleParallel();
    }
}