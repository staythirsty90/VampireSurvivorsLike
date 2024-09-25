using System.Collections;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class Debug_Gizmos : MonoBehaviour {
    public static bool drawDebug;
    private void Update() {
        if(Input.GetKeyDown(KeyCode.Alpha1)) {
            drawDebug = !drawDebug;
        }
    }

#if UNITY_EDITOR

    public GUIStyle style = new();

    EntityManager em;
    NativeArray<Entity> entities;

    StageManager StageManager;

    IEnumerator Start() {
        while(World.DefaultGameObjectInjectionWorld == null) {
            yield return null;
        }

        // For drawing the rect Bounds of the current stage.
        StageManager = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<StageManager>();

        showEnemyHealth = PlayerPrefs.GetInt(nameof(showEnemyHealth)) == 1;
        showEnemyBounds = PlayerPrefs.GetInt(nameof(showEnemyBounds)) == 1;
        showEnemyRadius = PlayerPrefs.GetInt(nameof(showEnemyRadius)) == 1;

        showMissileBounds = PlayerPrefs.GetInt(nameof(showMissileBounds)) == 1;
        showMissileRadius = PlayerPrefs.GetInt(nameof(showMissileRadius)) == 1;
        showMissileEntity = PlayerPrefs.GetInt(nameof(showMissileEntity)) == 1;
    }

    bool showEnemyHealth;
    bool showEnemyBounds;
    bool showEnemyRadius;

    bool showMissileBounds;
    bool showMissileRadius;
    bool showMissileEntity;

    GUIStyle toggleStyle;

    private void OnGUI() {
        if(!drawDebug)
            return;
        
        toggleStyle = new GUIStyle(GUI.skin.toggle);
        toggleStyle.fontSize = 20;
        toggleStyle.normal.textColor = Color.white;

        GUILayout.Space(200);

        GUILayout.BeginVertical();
        GUILayout.Label("Enemy Settings");
        showEnemyHealth = (GUILayout.Toggle(showEnemyHealth, "Show Enemy Health", toggleStyle));
        showEnemyBounds = (GUILayout.Toggle(showEnemyBounds, "Show Enemy Bounds", toggleStyle));
        showEnemyRadius = (GUILayout.Toggle(showEnemyRadius, "Show Enemy Radius", toggleStyle));
        PlayerPrefs.SetInt(nameof(showEnemyHealth), showEnemyHealth ? 1 : 0);
        PlayerPrefs.SetInt(nameof(showEnemyBounds), showEnemyBounds ? 1 : 0);
        PlayerPrefs.SetInt(nameof(showEnemyRadius), showEnemyRadius ? 1 : 0);
        GUILayout.EndVertical();

        GUILayout.Space(25);

        GUILayout.BeginVertical();
        GUILayout.Label("Missile Settings");
        showMissileRadius  = GUILayout.Toggle(showMissileRadius,  "Show Missile Radius", toggleStyle);
        showMissileBounds  = GUILayout.Toggle(showMissileBounds,  "Show Missile Bounds", toggleStyle);
        showMissileEntity  = GUILayout.Toggle(showMissileEntity,  "Show Missile Entity Info", toggleStyle);
        PlayerPrefs.SetInt(nameof(showMissileRadius ), showMissileRadius  ? 1 : 0);
        PlayerPrefs.SetInt(nameof(showMissileBounds ), showMissileBounds  ? 1 : 0);
        PlayerPrefs.SetInt(nameof(showMissileEntity ), showMissileEntity  ? 1 : 0);
        GUILayout.EndVertical();
    }

    private unsafe void OnDrawGizmos() {
        if(!drawDebug) {
            //Debug.Log("drawDebug is disabled!");
            return;
        }

        var xMax = ScreenBounds.InnerRect.xMax;
        var xMin = ScreenBounds.InnerRect.xMin;
        var yMax = ScreenBounds.InnerRect.yMax;
        var yMin = ScreenBounds.InnerRect.yMin;
        
        var outerRect = ScreenBounds.OuterRect;
        
        var stageRect  = StageManager != null && StageManager.CurrentStage != null ? StageManager.CurrentStage.Rect : new Rect();

        const float z = -3f;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(ScreenBounds.InnerRect.center, ScreenBounds.InnerRect.size);
        Gizmos.DrawWireCube(outerRect.center, outerRect.size);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(stageRect.center, stageRect.size);


        Handles.Label(new Vector3(xMax, yMax, z), $"Max: {xMax:0.00},{yMax:0.00}", style);
        Handles.Label(new Vector3(xMin, yMin, z), $"Min: {xMin:0.00},{yMin:0.00}", style);

        Handles.Label(new Vector3(outerRect.xMax, outerRect.yMax, z), $"Max: {outerRect.xMax:0.00},{outerRect.yMax:0.00}", style);
        Handles.Label(new Vector3(outerRect.xMin, outerRect.yMin, z), $"Min: {outerRect.xMin:0.00},{outerRect.yMin:0.00}", style);

        Handles.Label(new Vector3(stageRect.xMax, stageRect.yMax, z), $"stageMax: {stageRect.xMax},{stageRect.yMax}", style);
        Handles.Label(new Vector3(stageRect.xMin, stageRect.yMin, z), $"stageMin: {stageRect.xMin},{stageRect.yMin}", style);

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(Camera.main.ScreenToWorldPoint(Input.mousePosition), 0.5f);

        if(World.DefaultGameObjectInjectionWorld == null) {
            //Debug.Log("Debug gizmos coulnd't find a World!");
            return;
        }

        em = World.DefaultGameObjectInjectionWorld.EntityManager;
        entities = em.GetAllEntities(Allocator.TempJob);

        var playerCharacter = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlayerCharacter>();
        if(playerCharacter != null) {
            foreach(var cd in playerCharacter.ContactPoints) {
                var pos = cd.contactPos;
                var dir2 = new float3(cd.direction.y, -cd.direction.x, 0);
                var dir3 = new float3(-cd.direction.y, cd.direction.x, 0);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(pos, 0.1f);
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(new UnityEngine.Ray(pos, dir2));
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(new UnityEngine.Ray(pos, dir3));
                Gizmos.color = Color.red;
                Gizmos.DrawRay(cd.obstPos, math.normalize(cd.direction) * cd.obstRadius);
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(cd.contactPos, playerCharacter.PlayerMovementInput);
            }
        }

        foreach(var e in entities) {
            var color = Gizmos.color;

            if(em.HasComponent<Obstructible>(e)) {
                var status = em.GetComponentData<State>(e);
                if(!status.isActive)
                    continue;
                var ltf = em.GetComponentData<LocalTransform>(e);
                string entity = $"Idx:{e.Index}";
                ltf.Position.y += 1;
                Handles.Label(ltf.Position, entity, style);
                ltf.Position.y -= 1;

                //Gizmos.DrawWireSphere(ltf.Position, 0.3f);

                Gizmos.color = Color.green;
                if(em.HasComponent<PhysicsCollider>(e)) {
                    var pc = em.GetComponentData<PhysicsCollider>(e);
                    if(pc.ColliderPtr->Type == ColliderType.Box) {
                        var box = (Unity.Physics.BoxCollider*)pc.ColliderPtr;
                        Gizmos.DrawWireCube(box->Center + ltf.Position, box->Size);
                        //Debug.Log($"Drawing box for entity index: {e.Index}");
                    }
                    else if(pc.ColliderPtr->Type == ColliderType.Sphere) {
                        var sphere = (Unity.Physics.SphereCollider*)pc.ColliderPtr;
                        Gizmos.DrawWireSphere(sphere->Geometry.Center + ltf.Position, sphere->Radius * ltf.Scale);
                    }
                }

                if(em.HasComponent<BoundingBox>(e)) {
                    var bb = em.GetComponentData<BoundingBox>(e);
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireCube(bb.center, bb.size);
                }
            }

            Gizmos.color = color;

            if(em.HasComponent<PlayerAnimation>(e)) {
                var pc = em.GetComponentData<PhysicsCollider>(e);
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(((Unity.Physics.SphereCollider*)pc.ColliderPtr)->Center, 0.2f); // TODO: Assuming the player position is 0,0,0 and radius is 0.2f.
            }

            else if(em.HasComponent<PhysicsCollider>(e)) {
                var ltf = em.GetComponentData<LocalTransform>(e);
                Gizmos.color = Color.green;
                var pc = em.GetComponentData<PhysicsCollider>(e);
                var sphere = (Unity.Physics.SphereCollider*)pc.ColliderPtr;
                if(sphere != null) {
                    Gizmos.DrawWireSphere(sphere->Geometry.Center + ltf.Position, sphere->Radius * ltf.Scale);
                }

                Gizmos.color = Color.red;
            }

            if(em.HasComponent<Enemy>(e)) {
                var status = em.GetComponentData<State>(e);
                if(!status.isActive)
                    continue;
                var enemy = em.GetComponentData<Enemy>(e);
                var enemyTranslation = em.GetComponentData<LocalTransform>(e);
                var pos = enemyTranslation.Position;
                var enemybb = em.GetComponentData<BoundingBox>(e);

                if(showEnemyBounds) {
                    Gizmos.DrawWireCube(enemybb.center, enemybb.size);
                }

                if(showEnemyHealth) {
                    string health = $"{enemy.Health}";
                    Handles.Label(pos, health, style);
                }
                if(showEnemyRadius) {
                    if(em.HasComponent<PhysicsCollider>(e)) {
                        var tfpos = em.GetComponentData<LocalTransform>(e).Position;
                        Gizmos.DrawWireSphere(tfpos, Utils.GetRadius(em.GetComponentData<PhysicsCollider>(e).ColliderPtr));
                    }
                }
                string entity = $"Idx:{e.Index}";
                pos.y += 1;
                Handles.Label(pos, entity, style);
            }

            if(em.HasComponent<Missile>(e)) {

                var status = em.GetComponentData<State>(e);
                if(!status.isActive )
                    continue;

                var missile = em.GetComponentData<Missile>(e);
                var missilebb = em.GetComponentData<BoundingBox>(e);
                var ltf = em.GetComponentData<LocalTransform>(e);
                var pos = ltf.Position;
                var scale = ltf.Scale;

                // Draw some missile directional rays.
                Gizmos.color = Color.green;
                Gizmos.DrawRay(new UnityEngine.Ray(pos, missile.direction));
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(new UnityEngine.Ray(pos, missile.target - pos));
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(new UnityEngine.Ray(pos, (missile.target - pos) - missile.direction));

                var angle = math.degrees(math.atan2(missile.direction.y, missile.direction.x)) - 90;
                var dir = MissileSpawnSystem.AngleToDirection(angle);
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(new UnityEngine.Ray(pos, dir));

                if(showMissileBounds) {
                    var bc = missilebb.center;
                    bc.z = pos.z;

                    if(missile.HitType == MissileHitType.AoE_RectRotation) {
                        //
                        // TODO: bounding box center is not correct for AoE_RectRotation, it should be the same
                        // as the LocalTransform position...
                        //
                        var ra          = missilebb.rotationAngle;
                        var center      = missilebb.pivot;
                        var rot         = Matrix4x4.TRS(center, quaternion.Euler(0, 0, math.radians(ra)), Vector3.one);
                        Gizmos.matrix   = rot;
                        var size        = missilebb.size;
                        
                        Gizmos.DrawWireCube(Vector3.zero, size);
                        
                        Gizmos.matrix   = Matrix4x4.identity;
                    }
                    else {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireCube(bc, missilebb.size);
                    }
                }

                Gizmos.color = Color.red;

                if(showMissileRadius) {
                    Gizmos.DrawWireSphere(pos, missile.Radius);
                }

                if(showMissileEntity) {
                    string entity = $"{e}\n{missile.direction}";
                    pos.y += 1;
                    Handles.Label(pos, entity, style);
                }
            }
        }
        entities.Dispose();
    }
#endif
}