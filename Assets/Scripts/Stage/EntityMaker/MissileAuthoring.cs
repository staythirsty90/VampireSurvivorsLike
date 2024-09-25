using Unity.Entities;
using UnityEngine;

public struct MissilePrefabTag : IComponentData { }

public class MissileAuthoring : MonoBehaviour {
    class ConfigBaker : Baker<MissileAuthoring> {
        public override void Bake(MissileAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new NonUniformScale { Value = new Unity.Mathematics.float3(1,1,1)});
            AddComponent(entity, new MissilePrefabTag());
        }
    }
}
