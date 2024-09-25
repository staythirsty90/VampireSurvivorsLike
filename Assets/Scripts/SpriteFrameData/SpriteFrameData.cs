using Unity.Entities;

public struct SpriteFrameData : IComponentData {
    public bool autoUpdate;
    public int currentFrame;
    public int frameCount;
    public float frameTimer;
    public float frameTimerMax;
    public int loopCount;
    public bool needsSpriteUpdated;
    public bool needsRendererUpdated;
}