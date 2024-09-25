using Unity.Entities;
using UnityEngine;
public struct ObstructiblePrefabTag : IComponentData { }

public class ObstructibleAuthoring : MonoBehaviour {
    class ConfigBaker : Baker<ObstructibleAuthoring> {
        public override void Bake(ObstructibleAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new ObstructiblePrefabTag());
            AddComponent(entity, new NonUniformScale { Value = new Unity.Mathematics.float3(1,1,1)});
        }
    }
}