using UnityEngine;
using Unity.Collections;
using UnityEngine.U2D;
using System.Collections.Generic;
using Unity.Mathematics;
using System;

[Serializable]
public struct SpriteData {
    public float3 size;
    public float3 center;
    public float2 pivot;
}

public class SpriteDB : MonoBehaviour {
    [SerializeField]
    SpriteAtlas spriteAtlas;
    public static SpriteDB Instance;
    Sprite[] sprites;
    readonly Dictionary<FixedString32Bytes, Sprite> stringToSprite = new();
    /// <summary>
    /// Sprite name and SpriteData
    /// </summary>
    public NativeHashMap<FixedString32Bytes, SpriteData> SpriteSizeHashMap;

    readonly Dictionary<Guid, List<Sprite>> guidToSpriteList = new();

    private void Awake() {
        if(Instance == null) {
            Instance = this;
        }
        else {
            Destroy(Instance.gameObject);
            Instance = this;
        }

        SpriteSizeHashMap = new NativeHashMap<FixedString32Bytes, SpriteData>(spriteAtlas.spriteCount, Allocator.Persistent);

        var spriteCount = spriteAtlas.spriteCount;
        sprites = new Sprite[spriteCount];
        spriteAtlas.GetSprites(sprites);

        foreach(var s in sprites) {
            s.name = s.name.Replace("(Clone)", "");
            stringToSprite.TryAdd(s.name, s);
            var data = new SpriteData {
                size = s.bounds.size,
                center = s.bounds.center,
                pivot = s.pivot,
            };
            SpriteSizeHashMap.Add(s.name, data);
            //Debug.Log(s.name);
            //Debug.Log($"--Adding {s.name} to SpriteDatas");
        }
    }

    void TryInitSpriteFromGfx(in ID id, in Gfx gfx) {
        if(id.Guid == Guid.Empty) {
            Debug.Log($"id guid is empty for gfx ({gfx.spriteName})!!!");
            return;
        }
        if(guidToSpriteList.ContainsKey(id.Guid)) {
            Debug.Log($"id guid ({gfx.spriteName}) is already present for sprte db: {id.Guid}");
            return;
        }
        guidToSpriteList.Add(id.Guid, GetListNames(gfx.spriteName.ToString(), gfx.startingFrame));
    }

    public void GameEvent_OnEntitiesInitialized() {
        foreach(var kvp in Missiles.MissileTable.AsReadOnly()) {
            TryInitSpriteFromGfx(kvp.Value.id, kvp.Value.gfx);
        }
        foreach(var kvp in Enemies.Table.AsReadOnly()) {
            TryInitSpriteFromGfx(kvp.Value.id, kvp.Value.gfx);
        }
        foreach(var kvp in PickUps.Table.AsReadOnly()) {
            TryInitSpriteFromGfx(kvp.Value.id, kvp.Value.gfx);
        }
    }

    List<Sprite> GetListNames(string name, byte start = 0) {
        var l = new List<Sprite>();

        if(stringToSprite.ContainsKey(name)) { // If the name matches as it is then this is most likely a single sprite without animation.
            l.Add(stringToSprite[name]);
            //Debug.Log(stringToSprite[name]);
            return l;
        }

        while(stringToSprite.ContainsKey($"{name}_{start}")) { // Otherwise check for the animation sprites using "name_0, name_1, etc".
            var n = stringToSprite[$"{name}_{start}"];
            //Debug.Log(n);
            l.Add(n);
            start++;
        }
        return l;
    }

    private void OnDestroy() {
        SpriteSizeHashMap.Dispose();
    }

    public Sprite Get(string name) {
        //Debug.Assert(sprites != null);
        //Debug.Assert(sprites.Length != 0);
        //Debug.Assert(!string.IsNullOrEmpty(name));
        return stringToSprite[name];
    }

    public Sprite Get(FixedString32Bytes name) {
        //Debug.Assert(sprites != null);
        //Debug.Assert(sprites.Length != 0);
        //Debug.Assert(name.Length != 0);
        return stringToSprite[name];
    }
    
    /// <summary>
    /// Return all the Sprites that start with name. NOTE this allocates memory and will trigger the Garbage Collector.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public List<Sprite> GetAllWithName(string name) {
        var sprites = new List<Sprite>();

        foreach(var kvp in stringToSprite) {
            if(kvp.Key.ToString().StartsWith(name))
                sprites.Add(kvp.Value);
        }

        return sprites;
    }
    
    /// <summary>
    /// Return all the Length of Sprites that start with name.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public int GetLengthOfName(string name) {
        var count = 0;
        foreach(var kvp in stringToSprite) {
            if(kvp.Key.ToString().StartsWith(name))
                count++;
        }
        return count;
    }

    /// <summary>
    /// Return the first Sprite and the length of all the sprites for a given Guid
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
    public (Sprite, int) Get(Guid guid) {
        //Debug.Log($"--About to get Sprite for guid:{guid}");
        //if(!new_cache.ContainsKey(guid)) {
        //    throw new Exception($"Can't Get sprite for guid: {guid}!");
        //}
        var n = guidToSpriteList[guid];
        //if(n.Count == 0)
        //    throw new Exception($"The sprites length for guid {guid} was 0!");
        return (n[0], n.Count);
    }


    /// <summary>
    /// Returns the Sprite of the currentFrame index for a given Guid
    /// </summary>
    /// <param name="guid"></param>
    /// <param name="currentFrame"></param>
    /// <returns></returns>
    public Sprite Get(Guid guid, int currentFrame) {
        var n = guidToSpriteList[guid];
        return n[currentFrame];
    }
}