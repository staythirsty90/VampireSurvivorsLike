using Unity.Entities;

public partial class GrabAllXPGems : SystemBase {
    public bool forceGrab;
    StageManager stageManager;

    protected override void OnCreate() {
        base.OnCreate();
        stageManager = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<StageManager>();
    }
    protected override void OnUpdate() {
        var frameStats = SystemAPI.GetSingleton<FrameStatsSingleton>();
        var xpgem_id = PickUps.XpGemID;

        if(frameStats.collectedMagnet || forceGrab) {
            frameStats.collectedMagnet = false;
            forceGrab = false;
            Entities.ForEach((ref Swoops swoops, in PickUp pickup, in State state, in ID id) => {
                if(!state.isActive) return;
                if(id.Guid != xpgem_id.Guid) return;
                if(swoops.swooping) return;
                if(swoops.grabbed) return;
                swoops.grabbed = true;
            }).ScheduleParallel();
        }

        if(frameStats.collectedRosary) {
            frameStats.collectedRosary = false;
            stageManager.CurrentWave.WaveEvents.Add(new WaveEvent {
                eventType = WaveEventType.KillEnemies,
            });
        }
     
        SystemAPI.SetSingleton(frameStats);
    }
}