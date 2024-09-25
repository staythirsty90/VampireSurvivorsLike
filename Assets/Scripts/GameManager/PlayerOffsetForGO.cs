using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlayerOffsetForGO : MonoBehaviour {
    public enum OffsetMode : byte {
        Off,
        Transform,
        Texture
    }

    [Serializable]
    class OffsetEntry {
        public Transform transform;
        public OffsetMode offsetMode = OffsetMode.Texture;
    }

    [SerializeField]
    List<OffsetEntry> Objects;
    List<Material> materials;
    PlayerCharacter PlayerCharacter;

    private void Awake() {
        materials = new List<Material>(Objects.Count);
        foreach(var go in Objects) {
            materials.Add(go.transform.GetComponent<SpriteRenderer>().material);
        }
    }

    public void Add(GameObject go, OffsetMode offsetMode = OffsetMode.Texture) {
        Objects.Add(new() { transform = go.transform, offsetMode = offsetMode });
        materials.Add(go.GetComponent<SpriteRenderer>().material);
    }

    void LateUpdate() {
        if(PlayerCharacter == null) {
            if(World.DefaultGameObjectInjectionWorld == null) {
                return;
            }
            PlayerCharacter = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlayerCharacter>();
            Debug.Assert(PlayerCharacter != null);
        }
        
        var playerMoveDelta = (Vector2)PlayerCharacter.PlayerMovementDelta.xy;
        if(playerMoveDelta.x == 0 &&  playerMoveDelta.y == 0) {
            return;
        }

        if(Time.timeScale == 0) {
            return;
        }

        var length = Objects.Count;

        if(length != materials.Count) {
            materials.Clear();
            foreach(var go in Objects) {
                materials.Add(go.transform.GetComponent<SpriteRenderer>().material);
            }
        }

        for(var i = 0; i < length; i++) {

            var obj = Objects[i];
            var offsetMode = obj.offsetMode;
            var texture = materials[i];

            switch(offsetMode) {
                case OffsetMode.Off:
                    break;
                case OffsetMode.Transform: {
                    obj.transform.position -= (Vector3)playerMoveDelta;
                }
                break;
                case OffsetMode.Texture: {
                    var scale = texture.mainTextureScale / obj.transform.localScale;
                    var scroll = playerMoveDelta * scale;
                    var offset = texture.mainTextureOffset += scroll;

                    if(offset.x >= 1f || offset.x <= -1f) {
                        var diff = offset.x - (1 * math.sign(offset.x));
                        texture.mainTextureOffset = new Vector2(diff, offset.y);
                    }
                    if(offset.y >= 1f || offset.y <= -1f) {
                        var diff = offset.y - (1 * math.sign(offset.y));
                        texture.mainTextureOffset = new Vector2(offset.x, diff);
                    }
                }
                break;
            }
        }
    }
}