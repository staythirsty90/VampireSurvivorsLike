using Unity.Entities;

// debug
[DisableAutoCreation]
public partial class Debug_KillAllEnemies : SystemBase {
    protected override void OnUpdate() {
        Entities.ForEach((ref Enemy enemy, in State state) => {
            if(state.isActive) {
                enemy.Health = 0;
            }
        }).Run();
    }
}