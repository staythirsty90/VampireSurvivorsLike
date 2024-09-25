using Unity.Entities;
using UnityEngine;
public struct DestructiblePrefabTag : IComponentData { }

public class DestructibleAuthoring : MonoBehaviour {
    class ConfigBaker : Baker<DestructibleAuthoring> {
        public override void Bake(DestructibleAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new NonUniformScale { Value = new Unity.Mathematics.float3(1,1,1)});
            AddComponent(entity, new DestructiblePrefabTag());
        }
    }
}
