using Unity.Entities;
using UnityEngine;

public struct PlayerPrefabTag : IComponentData { }

public class PlayerAuthoring : MonoBehaviour {
    class ConfigBaker : Baker<PlayerAuthoring> {
        public override void Bake(PlayerAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new NonUniformScale { Value = new Unity.Mathematics.float3(1,1,1)});
            AddComponent(entity, new PlayerPrefabTag());
        }
    }
}