using Unity.Entities;
using UnityEngine;

public struct PickUpPrefabTag : IComponentData { }

public class PickUpAuthoring : MonoBehaviour {
    class ConfigBaker : Baker<PickUpAuthoring> {
        public override void Bake(PickUpAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new NonUniformScale { Value = new Unity.Mathematics.float3(1,1,1)});
            AddComponent(entity, new PickUpPrefabTag());
        }
    }
}
