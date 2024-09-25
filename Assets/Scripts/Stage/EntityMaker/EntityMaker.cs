using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[DisableAutoCreation]
public partial class EntityMakerSystem : SystemBase {

    public bool Finished { get; private set; }
    
    static Unity.Mathematics.Random random;
    static byte missilesThatHitMeBufferSize;
    static byte damageNumbersBufferSize;
    static EntityLimits EntityLimits;
    
    Entity player;
    Entity talentTree;

    protected override void OnCreate() {
        base.OnCreate();

        Finished = false;
        random = new(1234);

        var a = typeof(MissilesThatHitMe).GetCustomAttribute<InternalBufferCapacityAttribute>();
        if(a != null) {
            missilesThatHitMeBufferSize = (byte)a.Capacity;
            //Debug.Log($"Setting missilesThatHitMeBufferSize to :{missilesThatHitMeBufferSize}");
        }

        a = typeof(DamageNumberData).GetCustomAttribute<InternalBufferCapacityAttribute>();
        if(a != null) {
            damageNumbersBufferSize = (byte)a.Capacity;
            //Debug.Log($"Setting damageNumbersBufferSize to :{damageNumbersBufferSize}");
        }

        EntityLimits = UnityEngine.Object.FindObjectOfType<EntityLimits>();
        Debug.Assert(EntityLimits);
    }

    public struct SphereColliderInfo : IComponentData {
        public float3 Center;
        public float Radius;
    }

    protected unsafe override void OnUpdate() {
        
        var em = EntityManager;

        // TODO:
        //
        // Update runs at least twice because we are querying for Baked entities that have not been baked yet. If
        // the Baked entities are not found we return and try again in the next frame. After this Update succeeds we disable
        // this system. (I don't know how to detect when the Baking process is completed.)

        if(!SystemAPI.TryGetSingleton<FrameStatsSingleton>(out var stats)) {
            // Create the frame stats singleton
            var frameStatsEntity = em.CreateEntity(typeof(FrameStatsSingleton));
            SetName(frameStatsEntity, em, "FRAME_STATS");
        }

        var enemiesQuery = SystemAPI.QueryBuilder().WithAll<Enemy>().WithNone<EnemyPrefabTag, Destructible>().Build();
        var rot_75degrees = quaternion.EulerXYZ(75, 0, 0);

        // enemies
        if(enemiesQuery.IsEmpty) {
            var count = EntityLimits.EnemyCount;

            var entities = SystemAPI.QueryBuilder().WithAll<EnemyPrefabTag>().WithNone<Destructible>().Build().ToEntityArray(Allocator.Temp);

            if(entities.Length == 0) {
                Debug.LogError("couldn't find enemy prefab entity!");
                return;
            }
            var instances = em.Instantiate(entities[0], count, Allocator.Temp);
            for(var i = 0; i < count; i++) {
                var entity = instances[i];
                em.RemoveComponent<EnemyPrefabTag>(entity);
                em.AddComponentData(entity, new Movement());
                em.AddComponentData(entity, new OffsetMovement());
                em.AddComponentData(entity, new DamageNumberIndex());
                SetAsEnemy(entity, Enemy.SpecType.Normal, em);
                em.SetComponentData(entity, new LocalTransform { Position = new float3(-500, -i, 0), Scale = 1f, Rotation = quaternion.identity });
                MakeBasicEntity(em, entity);
                SetName(entity,  em, "Enemy");
            }

            // elites
            count = EntityLimits.ElitesCount;
            instances = em.Instantiate(entities[0], count, Allocator.Temp);

            for(var i = 0; i < count; i++) {
                var entity = instances[i];
                em.RemoveComponent<EnemyPrefabTag>(entity);
                em.AddComponentData(entity, new Movement());
                em.AddComponentData(entity, new OffsetMovement());
                em.AddComponentData(entity, new DamageNumberIndex());
                SetAsEnemy(entity, Enemy.SpecType.Elite, em);
                em.SetComponentData(entity, new LocalTransform { Position = new float3(-501, -i, 0), Scale = 1f, Rotation = quaternion.identity });
                MakeBasicEntity(em, entity);
                SetName(entity, em, "Enemy - Elite - ");
            }

            // swarmers
            count = EntityLimits.SwarmerCount;
            entities = SystemAPI.QueryBuilder().WithAll<SwarmerPrefabTag>().Build().ToEntityArray(Allocator.Temp);
            instances = em.Instantiate(entities[0], count, Allocator.Temp);

            for(var i = 0; i < count; i++) {
                var entity = instances[i];
                em.RemoveComponent<SwarmerPrefabTag>(entity);
                em.AddComponentData(entity, new Movement());
                em.AddComponentData(entity, new OffsetMovement());
                em.AddComponentData(entity, new DamageNumberIndex());
                SetAsEnemy(entity, Enemy.SpecType.Swarmer, em);
                em.SetComponentData(entity, new LocalTransform { Position = new float3(-502, -i, 0), Scale = 1f, Rotation = quaternion.identity });
                MakeBasicEntity(em, entity);
                SetName(entity, in em, "Enemy - Swarmer - ");
            }
        }

        // missiles
        var missilesQuery = SystemAPI.QueryBuilder().WithAll<Missile>().WithNone<MissilePrefabTag>().Build();

        if(missilesQuery.IsEmpty) {
            var count = EntityLimits.MaximumMissiles;
            var entities = SystemAPI.QueryBuilder().WithAll<MissilePrefabTag>().Build().ToEntityArray(Allocator.Temp);

            if(entities.Length == 0) {
                Debug.LogError("couldn't find missile prefab entity!");
                return;
            }

            var instances = em.Instantiate(entities[0], count, Allocator.Temp);

            for(var i = 0; i < count; i++) {
                var entity = instances[i];
                em.RemoveComponent<MissilePrefabTag>(entity);
                em.AddComponent<Missile>(entity);
                em.AddComponent<PowerUpLink>(entity);
                em.AddComponentData(entity, new OffsetMovement());
                em.SetComponentData(entity, new LocalTransform { Position = new float3(-530, -i, 0), Scale = 1f, Rotation = quaternion.identity });
                MakeBasicEntity(em, entity);
                em.SetComponentData(entity, new SpriteFrameData() {
                    autoUpdate = true,
                    frameTimer = UnityEngine.Random.Range(0f, 1f),
                    frameTimerMax = 0.1f, // TODO: Hard ccoded.
                });
                SetName(entity, in em, "Missile");
            }
        }

        // destructibles
        var destructQuery = SystemAPI.QueryBuilder().WithAll<Destructible>().WithNone<DestructiblePrefabTag>().Build();

        if(destructQuery.IsEmpty) {
            var count = EntityLimits.DestructiblesCount;
            var entities = SystemAPI.QueryBuilder().WithAll<DestructiblePrefabTag>().Build().ToEntityArray(Allocator.Temp);

            if(entities.Length == 0) {
                Debug.LogError("couldn't find destructible prefab entity!");
                return;
            }

            var instances = em.Instantiate(entities[0], count, Allocator.Temp);

            for(var i = 0; i < count; i++) {
                var entity = instances[i];
                em.RemoveComponent<DestructiblePrefabTag>(entity);
                em.AddComponentData(entity, new Destructible());
                em.AddComponentData(entity, new OffsetMovement());
                em.AddComponentData(entity, new Movement { MoveType = MoveType.Stationary });
                SetAsEnemy(entity, Enemy.SpecType.Normal, em);
                em.SetComponentData(entity, new LocalTransform { Position = new float3(-540, -i, 0), Scale = 1f, Rotation = rot_75degrees });
                MakeBasicEntity(em, entity);
                SetName(entity, in em, "Destructible");

                if(SystemAPI.HasComponent<PhysicsCollider>(entity)) {
                    var pc = em.GetComponentData<PhysicsCollider>(entity);
                    if(pc.ColliderPtr->Type == ColliderType.Sphere) {
                        var sc = (Unity.Physics.SphereCollider*)pc.ColliderPtr;
                        em.AddComponentData(entity, new SphereColliderInfo { Center = sc->Center, Radius = sc->Radius });
                    }
                }
            }
        }

        // pickups
        var PickUpsQuery = SystemAPI.QueryBuilder().WithAll<PickUp>().WithNone<PickUpPrefabTag>().Build();

        if(PickUpsQuery.IsEmpty) {
            var pickUpPrefab = SystemAPI.QueryBuilder().WithAll<PickUpPrefabTag>().Build().ToEntityArray(Allocator.Temp);

            if(pickUpPrefab.Length == 0) {
                Debug.LogError("couldn't find PickUp prefab entity!");
                return;
            }
            MakeXpGems(quaternion.identity, count: EntityLimits.XpGemsCount, in em, in pickUpPrefab);
            MakePickUps(quaternion.identity, count: EntityLimits.OtherPickupsCount, in em, in pickUpPrefab);
            MakeTreasureChests(rot_75degrees, count: 10, in em, in pickUpPrefab);
        }

        {   // Create Talents
            var tc = new TalentTreeComponent();
            talentTree = EntityManager.CreateEntity();
            SetName(talentTree, EntityManager, $"Talent Tree - ");
            var methods = typeof(Talents).GetMethods();

            for(sbyte i = 0; i < methods.Length; i++) {
                var method = methods[i];
                if(method.ReturnType == typeof(Talent)) {
                    var talent = (Talent)method.Invoke(null, null);
                    talent.Cost = talent.CostPerRank + (talent.CurrentRank * talent.CostPerRank); // NOTE: Does this have to set this here?
                    Talent.UpdateDesc(ref talent);
                    EntityManager.AddBuffer<TalentTreeComponent>(talentTree);
                    var b = EntityManager.GetBuffer<TalentTreeComponent>(talentTree);
                    tc.talent = talent;
                    b.Add(tc);
                }
            }
        }

        // Create Player after Talents
        var playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerAnimation>().WithNone<PlayerPrefabTag>().Build();

        if(playerQuery.IsEmpty) {
            var players = SystemAPI.QueryBuilder().WithAll<PlayerPrefabTag>().Build().ToEntityArray(Allocator.Temp);

            if(players.Length == 0) {
                Debug.LogError("couldn't find player prefab entity!");
                return;
            }
            player = players[0];

            em.RemoveComponent<PlayerPrefabTag>(player);
            em.AddComponentData(player, new Experience { Level = 1, currentXPFactor = 5, defaultXPFactor = 5 });
            em.AddComponentData(player, new CharacterComponent());
            em.AddComponentData(player, new TalentTreeLink { talentTreeEntity = talentTree });
            em.AddComponentData(player, new PlayerAnimation());
            em.AddComponentData(player, new HitData { duration = 0.15f });
            em.AddComponentData(player, new SpriteFrameData() {
                autoUpdate = true,
                frameCount = 10,
                frameTimer = UnityEngine.Random.Range(0f, 1f),
                frameTimerMax = 0.15f,
            });
            SetName(player, in em, "Player");
        }

        {   // Create Weapons

            var methods = typeof(PowerUps).GetMethods();

            for(sbyte i = 0; i < methods.Length; i++) {
                var method = methods[i];
                if(method.ReturnType == typeof(PowerUp)) {
                    var powerup = (PowerUp)method.Invoke(null, null);
                    powerup.weaponIndex = i; // TODO: Bonus powerups affect the weapon index, see MissileOnDiedSystem.

                    var entity = EntityManager.CreateEntity();
                    SetName(entity, EntityManager, $"PowerUp - {powerup.name} - ");

                    foreach(var weapon in powerup.Weapons) {
                        var wc = new WeaponComponent() { Weapon = weapon };
                        wc.Weapon.Init(wc.Weapon.spawnOffset);

                        EntityManager.AddBuffer<WeaponComponent>(entity);
                        var b = EntityManager.GetBuffer<WeaponComponent>(entity);
                        b.Add(wc);

                        AddBuffer<WeaponMissileEntity>(entity, 0, EntityManager);
                        SetName(entity, EntityManager, $"Weapon - {powerup.name} - ");
                    }

                    powerup.Weapons = new FixedList4096Bytes<Weapon>();
                    var pu = new PowerUpComponent() { PowerUp = powerup };
                    EntityManager.AddComponentData(entity, pu);
                    EntityManager.AddBuffer<PowerUpBuffer>(player);
                    var c = EntityManager.GetBuffer<PowerUpBuffer>(player);
                    c.Add(new PowerUpBuffer { powerupEntity = entity });
                }
            }
        }

        // ***********
        Finished = true;
        Enabled  = false;
        // ***********
    }

    public static void SetName(Entity entity, in EntityManager em, string debugName = "") {
#if UNITY_EDITOR
        em.SetName(entity, $"{debugName} {entity}");
#endif
    }

    public static void SetAsEnemy(Entity entity, in Enemy.SpecType specType, in EntityManager em) {
        em.AddComponentData(entity, new Enemy { specType = specType });
        em.AddComponentData(entity, new Drops());
        AddBuffer<MissilesThatHitMe>(entity, missilesThatHitMeBufferSize, em);
        AddBuffer<DamageNumberData>(entity, damageNumbersBufferSize, em);
        AddBuffer<HitEffectBuffer>(entity, 3, em);
    }

    public static void AddBuffer<T>(Entity entity, byte bufferSize, in EntityManager em) where T : unmanaged, IBufferElementData {
        em.AddBuffer<T>(entity);
        var buffArr = em.GetBuffer<T>(entity);
        for(var j = 0; j < bufferSize; j++) {
            buffArr.Add(new T());
        }
    }

    private static void MakeBasicEntity(in EntityManager em, Entity entity) {
        em.AddComponentData(entity, new ID());
        em.AddComponentData(entity, new Gfx());
        em.AddComponentData(entity, new BoundingBox());
        em.AddComponentData(entity, new SpawnData());
        em.AddComponentData(entity, new State {
            minuteOfSpawn = -1,
            StatusEffects = new() {
                new() {
                    HitEffect = HitEffect.Freeze,
                },
                new() {
                    HitEffect = HitEffect.Knockback,
                }
            }
        });
        var spriteRenderer = em.GetComponentObject<SpriteRenderer>(entity, typeof(SpriteRenderer));
        spriteRenderer.sprite = SpriteDB.Instance.Get("question_mark");
        em.AddComponentData(entity, new SpriteFrameData() {
            autoUpdate = true,
            frameTimer = UnityEngine.Random.Range(0f, 1f),
            frameTimerMax = 0.2f, // TODO: Hard coded.
        });
    }

    private static void MakeXpGems(quaternion rot, in int count, in EntityManager em, in NativeArray<Entity> entities) {
        var instances = em.Instantiate(entities[0], count, Allocator.Temp);
        for(var i = 0; i < count; i++) {
            var entity = instances[i];
            em.RemoveComponent<PickUpPrefabTag>(entity);
            em.SetComponentData(entity, new LocalTransform { Position = new float3(-560, -2 * entity.Index, 0f), Scale = 1f, Rotation = rot });
            em.AddComponentData(entity, new State());
            em.AddComponentData(entity, new ID());
            em.AddComponentData(entity, new Gfx());
            em.AddComponentData(entity, new PickUp());
            em.AddComponentData(entity, new OffsetMovement());
            em.AddComponentData(entity, new Swoops {
                distance = 0.4f, // TODO: Thiis is more like time. The lower the value the faster the gems "swoop".
            });

            SetName(entity, in em, "PickUp");
        }
    }

    private static void MakePickUps(quaternion rot, in int count, in EntityManager em, in NativeArray<Entity> entities) {
        var instances = em.Instantiate(entities[0], count, Allocator.Temp);
        for(var i = 0; i < count; i++) {
            var entity = instances[i];
            em.RemoveComponent<PickUpPrefabTag>(entity);
            em.AddComponentData(entity, new State());
            em.AddComponentData(entity, new ID());
            em.AddComponentData(entity, new Gfx());
            em.SetComponentData(entity, new LocalTransform { Position = new float3(-570, -2 * entity.Index, 0f), Scale = 1f, Rotation = rot });
            em.AddComponentData(entity, new PickUp());
            em.AddComponentData(entity, new OffsetMovement());
            em.AddComponentData(entity, new Swoops {
                distance = 0.4f, // TODO: Thiis is more like time. The lower the value the faster the gems "swoop".
            });
            SetName(entity, in em, "PickUp");
        }
    }

    private static void MakeTreasureChests(quaternion rot, in int count, in EntityManager em, in NativeArray<Entity> entities) {
        var instances = em.Instantiate(entities[0], count, Allocator.Temp);
        for(var i = 0; i < count; i++) {
            var entity = instances[i];
            em.RemoveComponent<PickUpPrefabTag>(entity);
            em.SetComponentData(entity, new LocalTransform { Position = new float3(-571, -2 * entity.Index, 0f), Scale = 1f, Rotation = rot });
            em.AddComponentData(entity, new Treasure {
                grabDistance = 0.5f,
            });
            var sr = em.GetComponentObject<SpriteRenderer>(entity);
            sr.sprite = SpriteDB.Instance.Get("TreasureChest");
            var center = sr.bounds.center;
            var size = sr.sprite.bounds.size;
            em.AddComponentData(entity, new State());
            em.AddComponentData(entity, new OffsetMovement());
            em.AddComponentData(entity, new BoundingBox { center = center, size = size});
            SetName(entity, in em, "Treasure");
        }
    }

    public void MakeDarkLibrary(in Stage stage) {
        var ObstructibleQuery = SystemAPI.QueryBuilder().WithAll<Obstructible>().WithNone<ObstructiblePrefabTag>().Build();

        if(ObstructibleQuery.IsEmpty) {
            var entities = SystemAPI.QueryBuilder().WithAll<ObstructiblePrefabTag>().Build().ToEntityArray(Allocator.Temp);
            var em = EntityManager;
            var rot_75degrees = quaternion.EulerXYZ(0, 0, 0);

            if(entities.Length == 0) {
                Debug.LogError("couldn't find Obstructible prefab entity!");
                return;
            }

            // obstructibles
            var stageRect = stage.Rect;
            var count = EntityLimits.ObstructiblesCount;
            var instances = em.Instantiate(entities[0], count, Allocator.Temp);

            for(var i = 0; i < count; i++) {
                var entity = instances[i];
                var pos = new float3(random.NextFloat(stageRect.xMin, stageRect.xMax), random.NextFloat(stageRect.yMin, stageRect.yMax), 0);
                em.RemoveComponent<ObstructiblePrefabTag>(entity);
                em.AddComponentData(entity, new Obstructible { flags = Obstructible.Flags.DoNotWrapY });
                em.SetComponentData(entity, new LocalTransform { Position = pos, Rotation = rot_75degrees, Scale = 1f });
                em.AddComponentData(entity, new State());
                em.AddComponentData(entity, new OffsetMovement());

                var spriteRenderer = em.GetComponentObject<SpriteRenderer>(entity, typeof(SpriteRenderer));
                var name = stage.obstructibleSettings.spriteName;
                var spriteLength = SpriteDB.Instance.GetLengthOfName(name);
                var roll = random.NextInt(0, spriteLength);
                var sprite = SpriteDB.Instance.Get($"{name}_{roll}");

                spriteRenderer.sprite = sprite;

                var bb = new BoundingBox();
                bb.SetBounds(pos, sprite.bounds.size);
                em.AddComponentData(entity, bb);

                em.AddComponentData(entity, new Gfx { spriteName = sprite.name });

                var filter = CollisionFilter.Default;
                filter.BelongsTo = 1u << 3;
                filter.CollidesWith = 1 << 1 | 1 << 2; // TODO: Hard coded.

                var radius = roll == 0 ? 0.4f : 0.15f; // TODO: Get a proper radius for the sprite.
                pos = new float3(0, radius, 0);
                var c = Unity.Physics.SphereCollider.Create(new SphereGeometry { Center = pos, Radius = radius });
                c.Value.SetCollisionFilter(filter);
                em.AddComponentData(entity, new PhysicsCollider { Value = c });

                AddBuffer<MissilesThatHitMe>(entity, 32, em);
                SetName(entity, in em, "Obstructible");
            }


            // Create the bookcases. They are not actually Obstructibles, they have no collider.
            var blockers = 2;
            var bookcaselength = 13;
            var bookcaseSizeFactor = 2;
            count = bookcaselength * blockers + blockers;
            instances = em.Instantiate(entities[0], count, Allocator.Temp);

            var yMax = stageRect.yMax;
            var scale = 1.1f;
            var colliderYScale = 4.75f;
            var wallpos = new float3(0, yMax + colliderYScale * 0.5f * scale, 0);

            {
                for(var j = 0; j < blockers; j++) {
                    var ystart = j == 0 ? wallpos.y : -wallpos.y;
                    var yscale = scale;
                    for(var i = 0; i < bookcaselength; i++) {
                        var entity = instances[j * bookcaselength + i ];
                        var xstart = stageRect.xMin + (i * scale * bookcaseSizeFactor);
                        var pos = new float3(xstart, ystart, 0);
                        em.RemoveComponent<ObstructiblePrefabTag>(entity);
                        em.AddComponentData(entity, new Obstructible { flags = Obstructible.Flags.DoNotWrapY });
                        em.SetComponentData(entity, new LocalTransform { Position = pos, Rotation = rot_75degrees, Scale = yscale });
                        em.AddComponentData(entity, new State { isActive = true });
                        em.AddComponentData(entity, new OffsetMovement());

                        var spriteRenderer = em.GetComponentObject<SpriteRenderer>(entity, typeof(SpriteRenderer));
                        var name = "bookcase"; // TODO: Hard coded.
                        var spriteLength = SpriteDB.Instance.GetLengthOfName(name);
                        var roll = random.NextInt(0, spriteLength);
                        var sprite = SpriteDB.Instance.Get($"{name}_{roll}");

                        spriteRenderer.sprite = sprite;
                        em.AddComponentData(entity, new Gfx { spriteName = sprite.name });

                        var bb = new BoundingBox();
                        bb.SetBounds(pos, spriteRenderer.bounds.size * scale);
                        em.AddComponentData(entity, bb);

                        SetName(entity, in em, "Obstructible - Bookcases");
                    }
                }
            }

            // create the bookcase collider blockers
            {
                for(var i = 0; i < blockers; i++) {
                    var ystart = i == 0 ? wallpos.y : -wallpos.y;
                    var index = count - blockers + i;
                    var entity = instances[index];
                    var pos = new float3(0, ystart, 0);
                    var size = new float3(stageRect.width, colliderYScale, 1) * scale;

                    em.RemoveComponent<ObstructiblePrefabTag>(entity);
                    em.AddComponentData(entity, new Obstructible { flags = Obstructible.Flags.DoNotWrapY | Obstructible.Flags.DoNotWrapX });
                    em.AddComponentData(entity, new ObstructibleBlockerTag());
                    em.SetComponentData(entity, new LocalTransform { Position = pos, Rotation = rot_75degrees, Scale = 1 });
                    em.AddComponentData(entity, new State { isActive = true });
                    
                    var bb = new BoundingBox();
                    bb.SetBounds(pos, size);
                    em.AddComponentData(entity, bb);
                    em.AddComponentData(entity, new OffsetMovement { OffsetType = OffsetType.NoPlayerOffsetX});

                    var spriteRenderer = em.GetComponentObject<SpriteRenderer>(entity, typeof(SpriteRenderer));
                    var sprite = SpriteDB.Instance.Get($"none"); // TODO: Hard coded.
                    spriteRenderer.sprite = sprite;
                    em.AddComponentData(entity, new Gfx { spriteName = sprite.name });

                    var filter = CollisionFilter.Default;
                    filter.BelongsTo = 1u << 3;             // TODO: Hard coded numbers.
                    filter.CollidesWith = 1 << 1 | 1 << 2;  //

                    pos = new float3(0, 0, 0);
                    var c = Unity.Physics.BoxCollider.Create(new BoxGeometry { Center = pos, Size = size, Orientation = quaternion.identity });
                    c.Value.SetCollisionFilter(filter);
                    em.AddComponentData(entity, new PhysicsCollider { Value = c });

                    AddBuffer<MissilesThatHitMe>(entity, 32, em);
                    SetName(entity, in em, "Obstructible - Blocker");
                }
            }
        }
    }

    public void MakeForest(in Stage stage) {

        var ObstructibleQuery = SystemAPI.QueryBuilder().WithAll<Obstructible>().WithNone<ObstructiblePrefabTag>().Build();

        if(ObstructibleQuery.IsEmpty) {
            var entities = SystemAPI.QueryBuilder().WithAll<ObstructiblePrefabTag>().Build().ToEntityArray(Allocator.Temp);
            var em = EntityManager;
            var rot_75degrees = quaternion.EulerXYZ(0, 0, 0);

            if(entities.Length == 0) {
                Debug.LogError("couldn't find Obstructible prefab entity!");
                return;
            }

            // obstructibles
            var stageRect = stage.Rect;
            var count = EntityLimits.ObstructiblesCount;
            var instances = em.Instantiate(entities[0], count, Allocator.Temp);

            for(var i = 0; i < count; i++) {
                var entity = instances[i];
                var pos = new float3(random.NextFloat(stageRect.xMin, stageRect.xMax), random.NextFloat(stageRect.yMin, stageRect.yMax), 0);
                em.RemoveComponent<ObstructiblePrefabTag>(entity);
                em.AddComponentData(entity, new Obstructible());
                em.SetComponentData(entity, new LocalTransform { Position = pos, Rotation = rot_75degrees, Scale = 1f });
                em.AddComponentData(entity, new State());
                em.AddComponentData(entity, new OffsetMovement());

                var spriteRenderer = em.GetComponentObject<SpriteRenderer>(entity, typeof(SpriteRenderer));
                var name = stage.obstructibleSettings.spriteName;
                var spriteLength = SpriteDB.Instance.GetLengthOfName(name);
                var roll = random.NextInt(0, spriteLength);
                var sprite = SpriteDB.Instance.Get($"{name}_{roll}");

                spriteRenderer.sprite = sprite;

                var bb = new BoundingBox();
                bb.SetBounds(pos, sprite.bounds.size);
                em.AddComponentData(entity, bb);

                em.AddComponentData(entity, new Gfx { spriteName = sprite.name });

                var filter = CollisionFilter.Default;
                filter.BelongsTo = 1u << 3;             // TODO: Hard coded numbers. 
                filter.CollidesWith = 1 << 1 | 1 << 2;  //

                var radius = roll == 0 ? 0.4f : 0.15f; // TODO: Get a proper radius for the sprite.
                pos = new float3(0, radius, 0);
                var c = Unity.Physics.SphereCollider.Create(new SphereGeometry { Center = pos, Radius = radius });
                c.Value.SetCollisionFilter(filter);
                em.AddComponentData(entity, new PhysicsCollider { Value = c });

                AddBuffer<MissilesThatHitMe>(entity, 32, em);
                SetName(entity, in em, "Obstructible");
            }
        }
    }
}