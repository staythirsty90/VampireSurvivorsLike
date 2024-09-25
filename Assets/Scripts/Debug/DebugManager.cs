using ImGuiNET;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Unity.Transforms;
using Unity.Mathematics;
using System;
using System.Collections.Generic;

public partial class DebugManager : SystemBase {

    PlayerStats PlayerStats;
    PlayerGold PlayerGold;
    LootSystem LootSystem;
    public float OriginalCameraZoom { get; private set; }
    readonly FPSData FPSData = new();

    protected override void OnCreate() {
        base.OnCreate();
        ImGuiUn.Layout += ImGuiUn_Layout;

        PlayerStats = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlayerStats>();
        Debug.Assert(PlayerStats != null);

        PlayerGold = GameObject.FindObjectOfType<PlayerGold>();
        Debug.Assert(PlayerGold != null);

        LootSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LootSystem>();
        Debug.Assert(LootSystem != null);
        
        OriginalCameraZoom = Camera.main.orthographicSize;
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        ImGuiUn.Layout -= ImGuiUn_Layout;
    }

    static string __input = string.Empty;
    private void ImGuiUn_Layout() {
        //ImGui.ShowDemoWindow();

        ImGui.Begin("Frame Stats");
        ImGui.SetWindowFontScale(1.6f);
        if(ImGui.Button("R")) {
            FPSData.reset();
        }
        ImGui.SameLine();
        ImGui.Text(FPSData.update(UnityEngine.Time.realtimeSinceStartup));
        ImGui.End();

        
        ImGui.SetWindowFontScale(2.0f);
        ImGui.Text($"TimeScale ({UnityEngine.Time.timeScale:0.00}x)");
        ImGui.SameLine();
        if(ImGui.Button("Reset")) {
            UnityEngine.Time.timeScale = 1;
        }
        if(ImGui.Button("<<")) {
            UnityEngine.Time.timeScale -= 0.1f;
        }
        ImGui.SameLine();
        if(ImGui.Button("<")) {
            UnityEngine.Time.timeScale -= 0.01f;
        }
        ImGui.SameLine();
        if(UnityEngine.Time.timeScale == 0) {
            if(ImGui.Button("|>")) {
                UnityEngine.Time.timeScale = 1f;
            }
        }
        else {
            if(ImGui.Button("||")) {
                UnityEngine.Time.timeScale = 0;
            }
        }
        ImGui.SameLine();
        if(ImGui.Button(">")) {
            UnityEngine.Time.timeScale += 1;
        }
        ImGui.SameLine();
        if(ImGui.Button(">>")) {
            UnityEngine.Time.timeScale += 2;
        }

        if(World.DefaultGameObjectInjectionWorld != null) {
            
            if(ImGui.Button("Delete Enemies")) {
                Entities.ForEach((Entity e, in Enemy d) => {
                    EntityManager.DestroyEntity(e);
                }).WithStructuralChanges().WithoutBurst().Run();
            }

            if(ImGui.Button("Delete Missiles")) {
                Entities.ForEach((Entity e, in Missile m) => {
                    EntityManager.DestroyEntity(e);
                }).WithStructuralChanges().WithoutBurst().Run();
            }
            
            if(ImGui.Button("Delete PickUps")) {
                Entities.ForEach((Entity e, in PickUp m) => {
                    EntityManager.DestroyEntity(e);
                }).WithStructuralChanges().WithoutBurst().Run();
            }

            if(ImGui.Button("Delete Obstructibles")) {
                Entities.ForEach((Entity e, in Obstructible d) => {
                    EntityManager.DestroyEntity(e);
                }).WithStructuralChanges().WithoutBurst().Run();
            }

            ImGui.Text($"Give Gold ({PlayerGold.Gold})");
            ImGui.SameLine();
            if(ImGui.Button("+50")) {
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<GoldCollectedSystem>().RequestGold(50);
            }

            if(ImGui.Button("Kill All Enemies (K)") || (Input.GetKeyDown(KeyCode.K) && !ImGui.IsAnyItemFocused())) {
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Debug_KillAllEnemies>().Update();
            }

            if(ImGui.Button("Spawn Treasure (T)") || (Input.GetKeyDown(KeyCode.T) && !ImGui.IsAnyItemActive())) {
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<DebugSystems>().Debug_SpawnTreasure();
            }

            if(ImGui.Button("XP Magnet (G)") || (Input.GetKeyDown(KeyCode.G) && !ImGui.IsAnyItemActive())) {
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<GrabAllXPGems>().forceGrab = true;
            }
        }

        if(ImGui.Button("Give Bonus Rewards")) {
            GameObject.FindObjectOfType<LevelUpContainer>(true)?.GiveRewards(true);
            GameObject.FindObjectOfType<LevelUpContainer>(true)?.Show();
        }

        if(ImGui.Button("Reroll Rewards")) {
            GameObject.FindObjectOfType<LevelUpContainer>(true)?.RerollChoices();
        }

        if(ImGui.Button("Empty PowerUp Store")) {
            LootSystem.EmptyPowerUpStore();
        }

        if(ImGui.Button("Give All Weapons")) {
            Entities.ForEach((ref PowerUpComponent puc) => {
                var pu = puc.PowerUp;
                if(!pu._isWeapon) return;
                if(pu.level == 0) {
                    LootSystem.GivePowerUp(pu.name);
                }
            }).WithoutBurst().Run();
        }
        else if(ImGui.Button("Remove All Weapons")) {
            Entities.ForEach((ref PowerUpComponent puc) => {
                var pu = puc.PowerUp;
                if(!pu._isWeapon) return;
                if(pu.level != 0) {
                    LootSystem.RemovePowerUp(ref pu);
                }
                puc.PowerUp = pu;
            }).WithoutBurst().Run();
        }

        ImGui.NewLine();

        if(ImGui.TreeNode("Experience")) {
            var fields = typeof(PlayerExperience).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if(World.DefaultGameObjectInjectionWorld != null) {
                var pe = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlayerExperience>();
                for(int i = 0; i < fields.Length; i++) {
                    var field = fields[i];
                    if(field.FieldType != typeof(float) &&
                    field.FieldType != typeof(int) &&
                    field.FieldType != typeof(bool)) continue;
                    ImGui.Text($"{field.Name}: {field.GetValue(pe)}");
                }

                ImGui.Text($"Level: {SystemAPI.GetSingleton<Experience>().Level}");
                ImGui.SameLine();
                if(ImGui.Button("+1")) {
                    LevelUp(1);
                }
                ImGui.SameLine();
                if(ImGui.Button("+5")) {
                    LevelUp(5);
                }
                ImGui.SameLine();
                if(ImGui.Button("+50")) {
                    LevelUp(50);
                }
                ImGui.TreePop();
            }
        }

        var amount = 0f;
        Guid id = new();
        if(!SystemAPI.TryGetSingleton(out CharacterComponent cc)) return;
        var chara = cc.character;

        if(ImGui.TreeNode("Stats")) {
            var i = 0;
            var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            var mult = shift ? 5f : alt ? 0.1f : 1;
            foreach(var kvp in chara.CharacterStats.stats) {
                var stat = kvp.Value;
                ImGui.Text($"{stat.name}: {stat.value:0.00}");
                ImGui.SameLine();
                ImGui.PushID(i);
                if(ImGui.Button($"+{mult:0.0}")) {
                    id = kvp.Key;
                    amount = mult;
                }
                ImGui.SameLine();
                if(ImGui.Button($"-{mult:0.0}")) {
                    id = kvp.Key;
                    amount = -mult;
                }
                ImGui.PopID();
                i++;
            }
            if(id != Guid.Empty) {
                if(id.Equals(Stats.HealthID)) {
                    World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlayerHealSystem>().RequestHeal(amount);
                }
                else {
                    chara.CharacterStats.baseStats[id] += amount;
                    cc.character = chara;
                    SystemAPI.SetSingleton(cc);
                    Debug.Log("Setting character stats");
                }
            }
            ImGui.TreePop();
        }

        if(ImGui.TreeNode("PowerUps")) {
            ImGui.InputText(" ", ref __input, 30);
            if(ImGui.Button("Give PowerUp")) {
                var powerUp = LootSystem.GetPowerUp(__input);
                if(!powerUp.Equals(default(PowerUp))) {
                    LootSystem.GivePowerUp(powerUp.name);
                }
                else {
                    Debug.Log($"Couldn't find PowerUp {__input}!");
                }
            }

            if(ImGui.TreeNode("Equipped PowerUps")) {
                Entities.ForEach((ref PowerUpComponent puc) => {
                    var powerup = puc.PowerUp;
                    
                    if(powerup.level == 0)
                        ImGui.PushStyleColor(0, new Vector4(0.7f, 0, 0, 1));
                    else
                        ImGui.PushStyleColor(0, new Vector4(1, 1, 1, 1));

                    ImGui.Text($" ");
                    ImGui.Text($"{powerup.name} (Lvl {powerup.level}/{powerup.maxLevel})");
                    ImGui.PopStyleColor();
                    if(powerup.level != powerup.maxLevel) {
                        ImGui.PushID(powerup.weaponIndex);
                        if(ImGui.Button("Lvl Up")) {
                            LootSystem.LevelUpPowerUp(powerup.name);
                            //Debug.Log($"leveling up {powerup.name}");
                        }
                        ImGui.PopID();
                    }
                    ImGui.SameLine();
                    ImGui.PushID(powerup.weaponIndex);
                    if(ImGui.Button("Remove")) {
                        Debug.Log($"Removing {powerup.name}, weaponIndex: {powerup.weaponIndex}");
                        LootSystem.RemovePowerUp(ref powerup);
                        puc.PowerUp = powerup;
                    }
                    ImGui.PopID();

                    //ImGui.Text($"Duration: {powerup.baseStats.Duration}");
                    //ImGui.Text($"Duration Timer: {powerup._durationTimer}");
                    if(powerup.PowerUpType == PowerUpType.ChargedBuff)
                        ImGui.Text($"Charges: {powerup.baseStats.Charges}/{powerup.baseStats.Amount}");
                    //ImGui.Text($"Cooldown: {powerup.baseStats.Cooldown}");
                    //ImGui.Text($"Cooldown Timer: {powerup.Weapons[0]._cooldownTimer}");
                    
                }).WithoutBurst().Run();
                ImGui.TreePop();
            }
            ImGui.TreePop();
        }

        if(ImGui.TreeNode("Missiles")) {

            ImGui.Text("Add Missiles");
            ImGui.SameLine();
            if(ImGui.Button("+1")) {
                //EntityMaker.MakeMissiles(1); // TODO
            }
            ImGui.SameLine();
            if(ImGui.Button("+5")) {
                //EntityMaker.MakeMissiles(5); // TODO
            }
            ImGui.SameLine();
            if(ImGui.Button("+10")) {
                //EntityMaker.MakeMissiles(10); // TODO
            }
            ImGui.SameLine();
            if(ImGui.Button("+50")) {
                //EntityMaker.MakeMissiles(50); // TODO
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if(world == null)
                return;

            var em = world.EntityManager;
            var entities = em.GetAllEntities(Allocator.Temp);

            {
                var active = 0;
                foreach(var e in entities) {
                    if(em.HasComponent<Missile>(e)) {
                        var state = em.GetComponentData<State>(e);

                        if(state.isActive) {
                            active++;

                            //var m = em.GetComponentData<Missile>(e);
                            //ImGui.Text($"CurrentAngle:{m.currentAngle}");
                            //ImGui.Text($"SpinAngle:{m.spinAngle}");
                            //ImGui.Text($"Radius:{m.Radius}");
                            //ImGui.Text($"Damage:{m.Damage}");
                            //ImGui.Text($"Duration:{m.Duration}");
                            //ImGui.Text($"Piercing:{m.Piercing}");
                            //ImGui.Text($"Flags:{m.Flags}");
                            //ImGui.Text($"ScaleEnd:{m.ScaleEnd}");
                            //ImGui.Text($"Speed:{m.Speed}");
                            //ImGui.Text($"PlayerArea:{chara.CharacterStats.Get(Stats.AreaID).value}");
                            ////ImGui.Text($"PowerUpArea:{LootSystem.PowerUps[m.weaponIndex].baseStats.Area}");
                            //ImGui.NewLine();

                            //if(ImGui.CollapsingHeader("Bounds Debug")) {
                            //    var bb = em.GetComponentData<BoundingBox>(e);
                            //    ImGui.Text($"BoundBox");
                            //    ImGui.Text($"Center:{bb.center}");
                            //    ImGui.Text($"Size:{bb.size}");
                            //    ImGui.Text($"Extents:{bb.extents}");
                            //    ImGui.Text($"Max:{bb.max}");
                            //    ImGui.Text($"Min:{bb.min}");

                            //    var sr = em.GetComponentData<SRLink>(e).SpriteRenderer;
                            //    ImGui.Text($"Sprite Bounds");
                            //    ImGui.Text($"Center:{sr.sprite.bounds.center}");
                            //    ImGui.Text($"Size:{sr.sprite.bounds.size}");
                            //    ImGui.Text($"Extents:{sr.sprite.bounds.extents}");
                            //    ImGui.Text($"Max:{sr.sprite.bounds.max}");
                            //    ImGui.Text($"Min:{sr.sprite.bounds.min}");
                            //}
                        }
                    }
                }
                ImGui.NewLine();
                ImGui.Text($"Active Missiles: {active}");
            }
            ImGui.TreePop();

            entities.Dispose();
        }

        if(ImGui.TreeNode("Enemies")) {

            ImGui.Text("Add Enemies");
            ImGui.SameLine();
            if(ImGui.Button("+1")) {
                //EntityMaker.MakeEnemies(1); // TODO
            }
            ImGui.SameLine();
            if(ImGui.Button("+5")) {
                //EntityMaker.MakeEnemies(5); // TODO
            }
            ImGui.SameLine();
            if(ImGui.Button("+10")) {
                //EntityMaker.MakeEnemies(10); // TODO
            }
            ImGui.SameLine();
            if(ImGui.Button("+50")) {
                //EntityMaker.MakeEnemies(50); // TODO
            }
            ImGui.Text("Remove Enemies");
            ImGui.SameLine();
            if(ImGui.Button("-1")) {
                //EntityMaker.RemoveEnemies(1); // TODO
            }
            ImGui.SameLine();
            if(ImGui.Button("-5")) {
                //EntityMaker.RemoveEnemies(5); // TODO
            }
            ImGui.SameLine();
            if(ImGui.Button("-10")) {
                //EntityMaker.RemoveEnemies(10); // TODO
            }
            ImGui.SameLine();
            if(ImGui.Button("-50")) {
                //EntityMaker.RemoveEnemies(50); // TODO
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if(world == null)
                return;

            var em = world.EntityManager;
            var entities = em.GetAllEntities(Allocator.Temp).AsReadOnly();

            {
                var active = 0;
                foreach(var e in entities) {
                    if(em.HasComponent<Enemy>(e)) {
                        var state = em.GetComponentData<State>(e);

                        if(state.isActive) {
                            active++;

                            //if(ImGui.CollapsingHeader("Bounds Debug")) {
                            //    var bb = em.GetComponentData<BoundingBox>(e);
                            //    ImGui.Text($"BoundBox");
                            //    ImGui.Text($"Center:{bb.center}");
                            //    ImGui.Text($"Size:{bb.size}");
                            //    ImGui.Text($"Extents:{bb.extents}");
                            //    ImGui.Text($"Max:{bb.max}");
                            //    ImGui.Text($"Min:{bb.min}");

                            //    var sr = em.GetComponentData<SRLink>(e).SpriteRenderer;
                            //    ImGui.Text($"Sprite Bounds");
                            //    ImGui.Text($"Center:{sr.sprite.bounds.center}");
                            //    ImGui.Text($"Size:{sr.sprite.bounds.size}");
                            //    ImGui.Text($"Extents:{sr.sprite.bounds.extents}");
                            //    ImGui.Text($"Max:{sr.sprite.bounds.max}");
                            //    ImGui.Text($"Min:{sr.sprite.bounds.min}");
                            //}
                        }
                    }
                }
                ImGui.NewLine();
                ImGui.Text($"Active Enemies: {active}");
            }
            ImGui.TreePop();
        }

        if(ImGui.TreeNode("Swarmers")) {

            ImGui.Text("Add Swarmers");
            ImGui.SameLine();
            if(ImGui.Button("+1")) {
                //EntityMaker.MakeSwarmers(1); // TODO
            }
            ImGui.SameLine();
            if(ImGui.Button("+5")) {
                //EntityMaker.MakeSwarmers(5); // TODO
            }
            ImGui.SameLine();
            if(ImGui.Button("+10")) {
                //EntityMaker.MakeSwarmers(10); // TODO
            }
            ImGui.SameLine();
            if(ImGui.Button("+50")) {
                //EntityMaker.MakeSwarmers(50); // TODO
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if(world == null)
                return;

            var em = world.EntityManager;
            var entities = em.GetAllEntities(Allocator.Temp).AsReadOnly();

            {
                var active = 0;
                foreach(var e in entities) {
                    
                    // TODO: Get the amount of active swarmers.

                    //if(em.HasComponent<Swarmer>(e)) {
                    //    var state = em.GetComponentData<State>(e);

                    //    if(state.isActive) {
                    //        active++;
                    //    }
                    //}
                }
                ImGui.NewLine();
                ImGui.Text($"Active Swarmers: {active}");
            }
            ImGui.TreePop();
        }

        if(ImGui.TreeNode("Destructibles")) {

            ImGui.Text("Add Destructibles");
            ImGui.SameLine();
            if(ImGui.Button("+1")) {
                //EntityMaker.MakeDestructibles(1); // TODO
            }
            ImGui.SameLine();
            if(ImGui.Button("+5")) {
                //EntityMaker.MakeDestructibles(5); // TODO
            }
            ImGui.SameLine();
            if(ImGui.Button("+10")) {
                //EntityMaker.MakeDestructibles(10); // TODO
            }
            ImGui.SameLine();
            if(ImGui.Button("+50")) {
                //EntityMaker.MakeDestructibles(50); // TODO
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if(world == null)
                return;

            var em = world.EntityManager;
            var entities = em.GetAllEntities(Allocator.Temp).AsReadOnly();

            {
                var active = 0;
                foreach(var e in entities) {
                    if(em.HasComponent<Destructible>(e)) {
                        var state = em.GetComponentData<State>(e);

                        if(state.isActive) {
                            active++;
                        }
                    }
                }
                ImGui.NewLine();
                ImGui.Text($"Active Destructibles: {active}");
            }
            ImGui.TreePop();
        }

        if(ImGui.TreeNode("XpGems")) {

            ImGui.Text("Add XpGems");
            ImGui.SameLine();
            if(ImGui.Button("+1")) {
                //EntityMaker.MakeXpGems(1); // TODO
            }
            ImGui.SameLine();
            if(ImGui.Button("+5")) {
                //EntityMaker.MakeXpGems(5); // TODO
            }
            ImGui.SameLine();
            if(ImGui.Button("+10")) {
                //EntityMaker.MakeXpGems(10); // TODO
            }
            ImGui.SameLine();
            if(ImGui.Button("+50")) {
                //EntityMaker.MakeXpGems(50); // TODO
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if(world == null)
                return;

            var em = world.EntityManager;
            var entities = em.GetAllEntities(Allocator.Temp).AsReadOnly();

            {
                var active = 0;
                foreach(var e in entities) {
                    if(em.HasComponent<PickUp>(e)) {
                        var pu = em.GetComponentData<PickUp>(e);
                        if(em.GetComponentData<ID>(e).Guid != PickUps.XpGemID.Guid) continue;

                        var state = em.GetComponentData<State>(e);

                        if(state.isActive) {
                            active++;
                        }
                    }
                }
                ImGui.NewLine();
                ImGui.Text($"Active XpGems: {active}");
            }
            ImGui.TreePop();
        }
    }

    private static void LevelUp(uint amount) {
        World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlayerExperience>()?.DebugLevelUp(amount);
    }
    
    protected override void OnUpdate() {
        if(Input.GetKeyDown(KeyCode.Alpha6)) {
            PlayerGold.Gold += 1000;
        }

        var world = World.DefaultGameObjectInjectionWorld;
        if(world == null)
            return;

        if(Debug_Gizmos.drawDebug) {
            var em = world.EntityManager;
            var entities = em.GetAllEntities(Allocator.Temp).AsReadOnly();

#if UNITY_EDITOR || PLATFORM_STANDALONE_WIN

            var chara = SystemAPI.GetSingleton<CharacterComponent>().character;

            if(Input.GetKeyDown(KeyCode.Alpha0)) {
                var stat = chara.CharacterStats.stats[Stats.HealthID];
                stat.value = 0;
                chara.CharacterStats.stats[Stats.HealthID] = stat;
                return;
            }

            if(Input.GetKeyDown(KeyCode.Alpha2)) {
                var sys = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<MissileCollisionSystem>();
                sys.Enabled = !sys.Enabled;
            }

            if(Input.GetKeyDown(KeyCode.Alpha3)) {
                var sys = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
                sys.Enabled = !sys.Enabled;
            }

            if(Input.GetKeyDown(KeyCode.Alpha4)) {
                var sys = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<DrawDamageNumberMeshSystem>();
                sys.Enabled = !sys.Enabled;
            }

            if(Input.GetKeyDown(KeyCode.Alpha9)) {
                var stat = chara.CharacterStats.stats[Stats.HealthID];
                stat.value -= 10;
                chara.CharacterStats.stats[Stats.HealthID] = stat;
            }

            // TODO: This is some jank debug camera zooming.
            if(Input.mousePosition.x > 0 && Input.mousePosition.x <= Screen.width && Input.mousePosition.y > 0 && Input.mousePosition.y <= Screen.height) {
                var scrollInput = Input.GetAxis("Mouse ScrollWheel");
                if(scrollInput > 0f) {
                    var zoomFactor                  =  math.pow(1.1f, -scrollInput);
                    Camera.main.orthographicSize    *= zoomFactor;
                    var mousePos                    =  Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    var positionShift               =  mousePos - Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f, Camera.main.nearClipPlane));
                    positionShift.z                 =  0;
                    Camera.main.transform.position  += positionShift;
                }
                else if(scrollInput < 0f) {
                    Camera.main.orthographicSize += 50f * UnityEngine.Time.deltaTime * -scrollInput;
                }
            }

            if(Input.GetMouseButtonDown(2)) {
                Camera.main.orthographicSize = OriginalCameraZoom;
                Camera.main.transform.position = new Vector3(0, 0, -10);
            }

            if(Input.GetKeyDown(KeyCode.P)) {
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Debug_SpawnPickups>()?.Update();
            }

            if(Input.GetKeyDown(KeyCode.T)) {
                Entities.ForEach((SpriteRenderer sr, Entity e) => {
                    if(sr.sprite) {
                        Debug.Log($"{sr.sprite.name} - {e.Index}");
                    }
                }).WithoutBurst().Run();
            }
#endif
        }
    }
}

partial class DebugSystems : SystemBase {
    protected override void OnUpdate() {

    }
    public void Debug_SpawnTreasure() {

        World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<MissileCollisionSystem>()?.Complete();

        Entities
            .ForEach((Entity e, int entityInQueryIndex, ref State state, ref LocalTransform pickUpPosition, ref Treasure treasure) => {
                if(state.isActive) return;
                state.isActive = true;
                pickUpPosition.Position = new float3(3 + entityInQueryIndex, 0, 0);
                treasure.treasureSettings = TreasureSettings.Create();
            }).Run();
    }
}