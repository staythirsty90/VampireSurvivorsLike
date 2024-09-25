using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(Init1), OrderFirst = true)]
public partial class PlayerCharacter : SystemBase {
    public struct ContactData {
        public float3 contactPos;
        public float3 direction;
        public float3 obstPos;
        public float obstRadius;
        public int type;
    }
    public float3 PlayerMovementInput { get; private set; }
    public float3 PlayerMovementDelta { get; private set; }
    public float3 PlayerMovementTotal { get; private set; }
    bool IsFacingRight;
    TouchControl thumbControl;
    
    StageManager StageManager;
    public NativeList<ContactData> ContactPoints;

    protected override void OnCreate() {
        base.OnCreate();
        thumbControl = Object.FindObjectOfType<TouchControl>();
        Debug.Assert(thumbControl != null);

        StageManager = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<StageManager>();
        Debug.Assert(StageManager != null);

        if(!ContactPoints.IsCreated) {
            ContactPoints = new NativeList<ContactData> (Allocator.Persistent);
        }
    }

    protected override void OnStopRunning() {
        base.OnStopRunning();
        PlayerMovementDelta = 0;
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        ContactPoints.Dispose();
    }

    protected unsafe override void OnUpdate() {
        ContactPoints.Clear();
        var dt = UnityEngine.Time.deltaTime;
        PlayerMovementInput = GetMovementNormalized();
        var chara = SystemAPI.GetSingleton<CharacterComponent>().character;
        var moveSpeed = chara.CharacterStats.Get(Stats.MovementSpeedID).value;
        var config_playerspeed = StageManager.CurrentStage.configuration.PlayerPxSpeed;
        PlayerMovementDelta = moveSpeed * dt * PlayerMovementInput * config_playerspeed;

        if(PlayerMovementDelta.x > 0) {
            IsFacingRight = true;
        }
        else if(PlayerMovementDelta.x < 0) {
            IsFacingRight = false;
        }

        //
        // TODO: This is a jank way to check for things that block the players movement.
        //

        var playerColliderPos   = new float3(0, 0.10f, 0); // TODO: Get this from the Player Collider.
        var playerPosition      = float3.zero; // TODO: Get this from the Player.
        var playerRadius        = 0.2f; // TODO: Get this from Physics Shape.
        var moveDelta = PlayerMovementDelta;
        var playerInput = PlayerMovementInput;
        var contactDatas = ContactPoints;

        if(moveDelta.x != 0 || moveDelta.y != 0) {

            Entities
             .WithAny<Destructible, Obstructible>()
             .ForEach((Entity e, in State state, in LocalTransform position, in PhysicsCollider pc) => {
                 if(!state.isActive) return;
                 if(state.isDying) return;

                 if(pc.ColliderPtr->Type == ColliderType.Sphere) { 
                     var sphere = (Unity.Physics.SphereCollider*)pc.ColliderPtr;
                     var obstPos = position.Position + sphere->Geometry.Center;
                     var obstRadius = sphere->Geometry.Radius;
                     var futurePosition = playerColliderPos.xy + moveDelta.xy;
                     var results = Utils.IsCircleOverlappingCircle(futurePosition, playerRadius, obstPos.xy, obstRadius);

                     var didOverlap = results.Item1;
                     var distance = results.Item2;

                     if(didOverlap) {
                         distance = math.distance(playerColliderPos, obstPos); // recalc distance to undo extra moveDelta.xy added above
                         var dir = playerColliderPos - obstPos;
                         var mag = Vector3.Magnitude(playerInput);
                         var clockwise = new float3(dir.y, -dir.x, 0) * dt * moveSpeed * mag * 0.6f;
                         var counterclockwise = new float3(-dir.y, dir.x, 0) * dt * moveSpeed * mag * 0.6f;
                         var cw = 0;
                         var ccw = 1;
                         var type = cw;

                         if(playerInput.x < 0) {
                             if(dir.y >= 0) {
                                 type = ccw;
                             }
                         }
                         else if(playerInput.x > 0) {
                             if(dir.y < 0) {
                                 type = ccw;
                             }
                         }

                         if(playerInput.y < 0) {
                             if(dir.x < 0) {
                                 type = ccw;
                             }
                         }
                         else if(playerInput.y > 0) {
                             if(dir.x >= 0) {
                                 type = ccw;
                             }
                         }

                         if(type == cw) {
                             moveDelta = clockwise;
                         }
                         else if(type == ccw) {
                             moveDelta = counterclockwise;
                         }

                         var contactpt = playerColliderPos - (math.normalize(dir) * playerRadius);
                         contactDatas.Add(new ContactData { type = type, contactPos = contactpt, direction = dir, obstPos = obstPos, obstRadius = obstRadius });

                         //Debug.Log($"dir: {dir}, angle: {angle}, moveDelta: {moveDelta}, obstRadius: {obstRadius}, obstPosition: {pos}");
                     }
                 }

                 else if(pc.ColliderPtr->Type == ColliderType.Box) { 
                     var box = (Unity.Physics.BoxCollider*)pc.ColliderPtr;
                     //Debug.Log($"checking for boxes entity: {e.Index}");
                     var obstPos = position.Position + box->Geometry.Center;
                     var obstSize = box->Geometry.Size;
                     var futurePosition = playerColliderPos.xy + moveDelta.xy;
                     var results = Utils.IsCircleOverlappingBox2D(obstPos, box->Size, futurePosition, playerRadius);

                     var didOverlap = results.Item1;
                     var distance = results.Item2;

                     if(didOverlap) {
                         //Debug.Log($"overlapping with entity ({e.Index})");
                         moveDelta = 0;
                     }
                 }
             }).Run();
        }

        if(contactDatas.Length > 1) {
            var count = 0;
            foreach(var pt in contactDatas) {
                count += pt.type;
            }
            if (count % 2 != 0) {
                moveDelta = 0;
            }
        }

        PlayerMovementDelta = moveDelta;
        PlayerMovementTotal += PlayerMovementDelta;

        var fs = SystemAPI.GetSingleton<FrameStatsSingleton>();
        fs.isPlayerFacingRight = IsFacingRight;
        fs.playerMoveDelta = PlayerMovementDelta;
        fs.playerMoveTotal = PlayerMovementTotal;
        if(math.any(PlayerMovementDelta)) {
            fs.previousPlayerMoveDelta = PlayerMovementDelta;
        }
        else if (!math.any(fs.previousPlayerMoveDelta)) {
            fs.previousPlayerMoveDelta = IsFacingRight ? new float3(1, 0, 0) : new float3(-1,0,0);
        }
        SystemAPI.SetSingleton(fs);
        //Debug.Log($"PlayerMovementDelta: {PlayerMovementDelta}");
    }

    float3 GetMovementNormalized() {
        var joystickMovement = thumbControl.Movement;
        var hvMovement = Vector3.zero;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if(v > 0 || v < 0) {
            hvMovement += Vector3.up * v;
        }

        if(h > 0 || h < 0) {
            hvMovement += Vector3.right * h;
        }

        var movement = Vector3.zero;

        if(hvMovement.sqrMagnitude != 0) {
            movement = hvMovement;
        }

        if(joystickMovement.sqrMagnitude != 0) {
            movement = joystickMovement;
        }

        if(movement.sqrMagnitude + movement.sqrMagnitude > 1) {
            movement.Normalize();
        }

        return movement;
    }
}