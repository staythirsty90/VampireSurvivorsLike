using UnityEngine;
using Unity.Entities;
using System.Reflection;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.UI;
using System.Collections;
using Unity.Mathematics;

#if UNITY_EDITOR
using Unity.Scenes.Editor;
#endif

public enum GameState : byte {
    Title,
    World,
    Treasure,
    Paused,
    LevelUp,
    GameOver,
}

[RequireComponent(typeof(CharacterSelect))]
public class GameManager : MonoBehaviour {

    public bool skipCharacterAndStage = true;

    readonly Stack<GameState> StateStack = new ();
    GameState State = GameState.Title;

    CharacterSelect CharacterSelect;
    PlayerExperience PlayerExperience;
    LootSystem LootSystem;
    EntityMakerSystem EntityMakerSystem;

    TreasureContainer TreasureContainer;
    LevelUpContainer LevelUpContainer;
    PauseContainer PauseContainer;

    GameObject WorldUI;
    GameObject TitleUI;

    Character ch;

    private void Awake() {
        CharacterSelect = GetComponent<CharacterSelect>();
        Debug.Assert(CharacterSelect != null);
        
        Time.timeScale = 1; // NOTE: this is needed because the timeScale will be 0 when the player quits are gets game over...

        // TODO: Move this somewhere more appropriate?
        // Initialize 'game data' (IDs, Gfxes, Enemies, etc.)
        Stats.InitTables();
        Missiles.InitTables();
        Enemies.InitTables();
        PickUps.InitTables();
        Debug.Log($"Initialize Finished, frameCount:{Time.frameCount}");

        // NOTE: Initialize the World here otherwise in load() the Entities don't get converted and we end up waiting
        // for them forever...

        InitializeDefaultWorld();
        Debug.Log($"Default World Initialized! {World.DefaultGameObjectInjectionWorld} frameCount:{Time.frameCount}");

        AddMySystemsToList();
        Debug.Log($"Adding My Systems to List! Count:{MySystems.Count} frameCount:{Time.frameCount}");

        DisableMySystems(in MySystems);

        // Hide the World UI so we don't waste time rendering it. We disable it after creating the Systems because
        // some systems may depend on some Text gameObjects in the World UI.
        WorldUI = GameObject.Find("World UI");
        if(WorldUI != null && WorldUI.activeInHierarchy) {
            WorldUI.SetActive(false);
        }

        // Enable the TitleUI gameObject if it's inactive.
        TitleUI = GameObject.Find("Title UI");
        if(TitleUI != null && !TitleUI.activeInHierarchy) {
            TitleUI.SetActive(true);
        }
        StartCoroutine(initSystems());
    }

    private void OnEnable() {
        PlayGameButton.OnPlayGame += PlayGameButton_OnPlayGame;
        StartGameButton.OnStartGame += StartGameButton_OnStartGame;
        OptionsButton.OnOptionsPressed += OptionsButton_OnOptionsPressed;
        TalentsButton.OnTalentsPressed += TalentsButton_OnTalentsPressed;
        OptionsUI.OnOptionsClosed += OptionsUI_OnOptionsClosed;
    }

    private void OptionsUI_OnOptionsClosed(object sender, EventArgs e) {
        if(StateStack.Count != 0)
            State = StateStack.Pop();
    }

    private void TalentsButton_OnTalentsPressed(object sender, EventArgs e) {
        FindObjectOfType<TalentTreeUI>(true).Show();
    }

    private void OptionsButton_OnOptionsPressed(object sender, EventArgs e) {
        FindObjectOfType<OptionsUI>(true).Show();
        StateStack.Push(State);
        State = GameState.Title;
    }

    private void OnDisable() {
        PlayGameButton.OnPlayGame -= PlayGameButton_OnPlayGame;
        StartGameButton.OnStartGame -= StartGameButton_OnStartGame;
        OptionsButton.OnOptionsPressed -= OptionsButton_OnOptionsPressed;
        TalentsButton.OnTalentsPressed -= TalentsButton_OnTalentsPressed;
        OptionsUI.OnOptionsClosed -= OptionsUI_OnOptionsClosed;
    }

    private void StartGameButton_OnStartGame(object sender, EventArgs e) {
        FindObjectOfType<UIStageSelect>(true).Show();
    }

    public void HandlePause() {
        
        // NOTE: Prevent pausing or unpausing in the GaneOver state. Maybe we should just check if we are in the
        // GameOver state?

        if(State != GameState.Paused && State != GameState.World) {
            return;
        }

        if(PauseContainer.gameObject.activeSelf) {
            Unpause();
            //Debug.Log("Unpausing");
        }
        else {
            Pause();
            //Debug.Log("Pausing");
        }
    }

    private void Unpause() {
        PauseContainer.Hide();
        SetMySystemsAndTimescale(true, 1f);
        State = GameState.World;
    }

    private void Pause() {
        PauseContainer.Show();
        SetMySystemsAndTimescale(false, 0f);
        State = GameState.Paused;
    }

    private void PlayGameButton_OnPlayGame(object sender, EventArgs e) {
        OnPlayGame();
    }

    public void OnPlayGame() {
        StartCoroutine(PlayGame());
    }

    IEnumerator initSystems() {
        // I am not sure how to detect when the Entity baking process is done. So I have to wait until the entities
        // are baked and then created by the EntityMakerSystem.
        EntityMakerSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EntityMakerSystem>();
        Debug.Assert(EntityMakerSystem != null);

        while(!EntityMakerSystem.Finished) {
            EntityMakerSystem.Update();
            Debug.Log("GameManager::Waiting for Entities to be made...");
            if(EntityMakerSystem.Finished) break;
            yield return null;
        }

        Debug.Log("GameManager::Finished making Entities!"); 

        if(skipCharacterAndStage) {
            OnPlayGame();
        }
    }

    IEnumerator PlayGame() {
        yield return null;
        ThrottleFixedStepSimulation();

        PlayerExperience = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlayerExperience>();
        Debug.Assert(PlayerExperience != null);

        // Initialize the Player Loot after the GameData has been initialized
        // NOTE: why do we depend on the GameData to be initialized first?
        LootSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LootSystem>();
        Debug.Assert(LootSystem != null, "Couldn't find LootSystem!");

        // Initialize the Sprite Database after the GameData has been initialized
        SpriteDB.Instance.GameEvent_OnEntitiesInitialized();

#if UNITY_EDITOR
        // Disable Editor Live Conversion System
        World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EditorSubSceneLiveConversionSystem>().Enabled = false;
        Debug.Log($"EditorSubSceneLiveConversionSystem Disabled, frameCount:{Time.frameCount}");
#endif

        StageManager StageManager = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<StageManager>();
        if(skipCharacterAndStage) {
            StageManager.SetStage(Stages.GetDarkLibrary());
            CharacterSelect.SetCharacter(Characters.Knight());
            ch = CharacterSelect.GetCharacter();
        }
        else {
            var ss = FindObjectOfType<UIStageSelect>(true);
            Debug.Assert(ss != null);
            // NOTE: the stages are invoked before the enemies are initialized (their ID will be Guid.Empty).
            // so we reinitialize the stages here because by this time the enemy ID's are initialized so the
            // spawn enemy system will work
            ss.ReInitStages();
            StageManager.SetStage(ss.GetStage());
            // update the Player entities spritenames with the current selected character
            ch = CharacterSelect.GetCharacter();
            //Debug.Log($"Getting chosen character");
            
            if(ch.Equals(default(Character)))
                FindObjectOfType<UICharacterSelect>().GetDefaultCharacter();
        }

        // Create obstructibles for the selected stage.
        StageManager.CurrentStage.CreateObstructibles();

        // Create the stage floor.
        var offsetSystem = GetComponent<PlayerOffsetForGO>();
        var go = Instantiate(StageManager.CurrentStage.floorGameObject);
        offsetSystem.Add(go);

        // TOOD: improve this
        var sys = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<BeginInitializationEntityCommandBufferSystem>();
        var EntityManager = sys.EntityManager;
        var AllEntities = EntityManager.GetAllEntities(Allocator.TempJob);
        foreach(var e in AllEntities) {
            if(EntityManager.HasComponent<CharacterComponent>(e)) {
                EntityManager.SetComponentData(e, new CharacterComponent { character = ch });
                EntityManager.SetComponentData(e, new NonUniformScale { Value = ch.spriteScale });
                Debug.Log("setting character");
            }
        }

        // Reset ScreenBounds rects as they are static.
        ScreenBounds.ResetRectCenters();

        AllEntities.Dispose();

        SetPlayerSpriteNames(ch);
        StageManager.UpdatePickupDropTable();
        LootSystem.InitPowerUpStore();

        // NOTE: is this the best place to initialize the selected character's starting powerup?
        if(ch.startingWeapons.Length != 0) {
            foreach(var powerup in ch.startingWeapons) {
                LootSystem.GivePowerUp(powerup);
            }
        }

        TreasureContainer = FindObjectOfType<TreasureContainer>(true);
        Debug.Assert(TreasureContainer, "Couldn't find TreasureContainer!");

        LevelUpContainer = FindObjectOfType<LevelUpContainer>(true);
        Debug.Assert(LevelUpContainer, "Couldn't find LevelUpContainer!");

        PauseContainer = FindObjectOfType<PauseContainer>(true);
        Debug.Assert(PauseContainer, "Couldn't find PauseContainer!");

        // NOTE: forced to bind this here because PauseContainer will be null in HandlePause()...
        FindObjectOfType<PauseGameButton>(true).GetComponent<Button>().onClick.AddListener(() => { HandlePause(); });
        
        TitleUI.SetActive(false);
        WorldUI.SetActive(true);

        DisableMySystems(in MySystems, true);

        State = GameState.World;

#if UNITY_EDITOR
        // debug stuff
        //if(UnityEditor.Selection.activeGameObject == null) {
        //    UnityEditor.Selection.SetActiveObjectWithContext(GameObject.Find("DEBUG_GIZMOS"), null);
        //    Debug.Log("Selected Debug gizmos");
        //}
#endif
    }

    private static void DisableAllSystems() {
        foreach(var sys in World.DefaultGameObjectInjectionWorld.Systems) {
            sys.Enabled = false;
        }
        Debug.Log($"Disabled All Systems! frameCount: {Time.frameCount}");
    }

    private static void DisableMySystems(in List<ComponentSystemBase> MySystems, bool enable = false) {
        foreach(var sys in MySystems) {
            sys.Enabled = enable;
        }
        Debug.Log($"Disabled My Systems! frameCount: {Time.frameCount}");
    }

    void SetMySystemsAndTimescale(bool state, float timescale) {
        foreach(var sys in MySystems) {
            sys.Enabled = state;
        }
        Time.timeScale = timescale;
    }

    private static void SetPlayerSpriteNames(Character ch) {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var ents = em.GetAllEntities(Allocator.Temp);
        foreach(var e in ents) {
            if(em.HasComponent<PlayerAnimation>(e)) {
                em.SetComponentData(e, new PlayerAnimation() {
                    SpriteNames_Moving = ch.runSpriteNames,
                    SpriteNames_Idle = ch.idleSpriteNames,
                });
                Debug.Log($"Updated Player SpriteNames: {ch.idleSpriteNames[0]}");
                break;
            }
        }

        //World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<SpriteSystem>().Update(); // force sprite update

        ents.Dispose();
    }

    void InitializeDefaultWorld() {
        if(World.DefaultGameObjectInjectionWorld == null) {
            DefaultWorldInitialization.Initialize("Default World");
        }
    }

    private static void ThrottleFixedStepSimulation() {
        var fixedstepsys = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
        fixedstepsys.Timestep = 1 / 30f;
        fixedstepsys.World.MaximumDeltaTime = 0.01667f;
    }

    readonly List<ComponentSystemBase> MySystems = new();
    private void AddMySystemsToList() {
        foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies()) {
            foreach(Type t in a.GetTypes()) {
                // filter out unity systems
                if(t.Namespace != null && t.Namespace.StartsWith("Unity")) {
                    continue;
                }

                // filter out non systems
                if(!t.IsSubclassOf(typeof(SystemBase)) && !t.IsSubclassOf(typeof(ComponentSystemBase))) {
                    continue;
                }

                // filter out any systems we want to keep running when paused
                if(t.Name.StartsWith("Draw")) {
                    continue;
                }

                if(t.Name.StartsWith("EntityMaker")) {
                    continue;
                }

                var sys = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged(t);
                if(sys != null) MySystems.Add(sys);
            }
        }
    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.Alpha2)) {
            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlayerExperience>().DebugLevelUp(1);
        }

        switch(State) {
            case GameState.Title: {
            }
            break;

            case GameState.World: {
                if(Input.GetKeyDown(KeyCode.Escape)) {
                    Pause();
                }

                if(!ch.CharacterStats.stats.IsCreated || ch.CharacterStats.stats.Count == 0) {  // debugging
                    Debug.LogWarning($"Requested Players Stats but its null or empty, count: {ch.CharacterStats.stats.Count}");
                    return;
                }

                // TODO: Assert that the Character is not null.
                var health = ch.CharacterStats.Get(Stats.HealthID).value;
                //Debug.Log($"Playerhealth: {health}");
                if(health == 0) {
                    State = GameState.GameOver;
                    DisableAllSystems();
                    FindObjectOfType<GameOverContainer>(true).GameEvent_OnGameOver();
                    WorldUI.SetActive(false);
                }

                // Check for treasure
                if(TreasureContainer.TreasureSettings.level > 0) { // Transition into Treasure State
                    State = GameState.Treasure;
                    SetMySystemsAndTimescale(false, 0f);
                    TreasureContainer.Show();
                }

                // Check for Level Up
                if(PlayerExperience.waitingOnChoosePowerUp) {
                    State = GameState.LevelUp;
                    LevelUpContainer.GiveRewards();
                    LevelUpContainer.Show();
                    SetMySystemsAndTimescale(false, 0f);
                }

                else if(LevelUpContainer.IsShowing()) { // This is for bonus rewards.
                    State = GameState.LevelUp;
                    SetMySystemsAndTimescale(false, 0f);
                }
            }
            break;

            case GameState.Treasure: {
                if(TreasureContainer.TreasureSettings.level == 0) { // level 0 means the player closed the UI
                    State = GameState.World;
                    SetMySystemsAndTimescale(true, 1f);
                    TreasureContainer.Hide();
                }
            }
            break;

            case GameState.Paused: {
                if(Input.GetKeyDown(KeyCode.Escape)) {
                    Unpause();
                }
            }
            break;

            case GameState.LevelUp: {
                // Check for Bonus Rewards
                if(LevelUpContainer.PickedPowerUps.Count > 0) {
                    return;
                }

                // Check for Finished Level Up
                if(PlayerExperience.waitingOnChoosePowerUp)
                    return;

                if(PlayerExperience.TryLevelUp()) {
                    LootSystem.GenerateLoot();                
                    LevelUpContainer.GiveRewards();
                }
                else {
                    State = GameState.World;
                    LevelUpContainer.Hide();
                    SetMySystemsAndTimescale(true, 1f);
                }
            }
            break;
            case GameState.GameOver:
                break;
        }
    }
}