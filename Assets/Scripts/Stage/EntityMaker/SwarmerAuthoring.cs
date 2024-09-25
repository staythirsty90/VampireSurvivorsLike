using Unity.Entities;
using UnityEngine;

public struct SwarmerPrefabTag : IComponentData { }

public class SwarmerAuthoring: MonoBehaviour {
    class ConfigBaker : Baker<SwarmerAuthoring> {
        public override void Bake(SwarmerAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new NonUniformScale { Value = new Unity.Mathematics.float3(1, 1, 1) });
            AddComponent(entity, new SwarmerPrefabTag());
        }
    }
}