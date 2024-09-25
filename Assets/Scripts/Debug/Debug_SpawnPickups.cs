using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// debug
[DisableAutoCreation]
public partial class Debug_SpawnPickups : SystemBase {
    protected override void OnUpdate() {
        var AllPowerups      = GetComponentLookup<PowerUpComponent>(true);
        var query            = SystemAPI.QueryBuilder().WithAll<PowerUpComponent>().Build();
        var powerupsCount    = query.CalculateEntityCount();
        var powerupsEntities = query.ToEntityArray(Allocator.TempJob);
        var PickUpsCount     = PickUps.ids.Length;
        var index = 0;

        for(var i = 0; i < PickUpsCount; i++) {
            var sourceGuid = PickUps.ids[i].Guid;
            
            if(sourceGuid == PickUps.GivePowerUpID.Guid) // TODO: Handle this case.
                continue;

            var sourceArch = PickUps.Table[sourceGuid];
            var sourceGfx    = sourceArch.gfx;
            var set = false;

            Entities
                .ForEach((ref PickUp pickUp, ref LocalTransform ltf, ref State state, ref Gfx gfx, ref ID id) => {
                    if(state.isActive) return;
                    if(index >= PickUpsCount) return;
                    if(set) return;
                    pickUp = sourceArch.pickUp;
                    state.isActive = true;
                    state.needsSpriteAndParticleSystem = true;
                    ltf.Position = new float3(index - 3, 3, ltf.Position.z);
                    id.Guid = sourceGuid;
                    gfx.spriteName = sourceGfx.spriteName;
                    
                    ltf.Scale = sourceArch.pickUp.spriteScale;
                    if(ltf.Scale == 0) {
                        ltf.Scale = 1;
                    }

                    //pickUp.GivePowerUpName = AllPowerups[powerupsEntities[index]].PowerUp.name;
                    //gfx.spriteName = AllPowerups[powerupsEntities[index]].PowerUp.spriteName;
                    set = true;
                    index++;
                }).WithoutBurst().Run();
        }

        powerupsEntities.Dispose();
    }
}