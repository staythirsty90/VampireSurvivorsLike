using UnityEngine;

public class EntityLimits : MonoBehaviour {
    public int EnemyCount = 800;
    public int ElitesCount;
    public int SwarmerCount = 100;
    public int MaximumMissiles = 800;
    public int XpGemsCount = 400;
    public int OtherPickupsCount = 100;
    public int DestructiblesCount = 10;
    public int ObstructiblesCount = 64;
    public static float frameTimer = 0.0667f;

    public int GetAllEnemiesCount() {
        return EnemyCount + ElitesCount + SwarmerCount + DestructiblesCount;
    }
}