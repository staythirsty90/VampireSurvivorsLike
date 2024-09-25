using Unity.Entities;
using UnityEngine;

public struct EnemyPrefabTag : IComponentData { }

public class EnemyAuthoring: MonoBehaviour {
    class ConfigBaker : Baker<EnemyAuthoring> {
        public override void Bake(EnemyAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new NonUniformScale { Value = new Unity.Mathematics.float3(1,1,1)});
            AddComponent(entity, new EnemyPrefabTag());
        }
    }
}