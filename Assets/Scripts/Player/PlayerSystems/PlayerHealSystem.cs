using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class PlayerHealSystem : SystemBase {
    readonly Color emptyColor = new(0, 0, 0, 0);

    Image PlayerHealthBar;
    public NativeArray<float> requestHeal;
    float regenTickCounter = 0;

    protected override void OnCreate() {
        base.OnCreate();

        PlayerHealthBar = GameObject.Find("PlayerHealthBar").GetComponent<Image>();
        Debug.Assert(PlayerHealthBar != null);
        PlayerHealthBar.fillAmount = 1;

        if(!requestHeal.IsCreated)
            requestHeal = new NativeArray<float>(1, Allocator.Persistent);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        if(requestHeal.IsCreated)
            requestHeal.Dispose();
    }

    public void RequestHeal(float amount) {
        requestHeal[0] = amount;
    }

    protected override void OnUpdate() {
        var time            = UnityEngine.Time.time;
        var frameStats      = SystemAPI.GetSingleton<FrameStatsSingleton>();
        var chara           = SystemAPI.GetSingleton<CharacterComponent>();
        var stats           = chara.character.CharacterStats.stats;
        var Health          = stats[Stats.HealthID];
        var MaximumHealth   = stats[Stats.MaxHealthID];
        var HealthRegen     = stats[Stats.HealthRegenerationID];

        var regenHealthAmount = 0f;
        var heal = requestHeal[0];
        regenTickCounter += UnityEngine.Time.deltaTime;

        if(regenTickCounter >= 1f) { // apply health regeneration once per second
            regenTickCounter = 0;
            regenHealthAmount = HealthRegen.value;
        }

        Entities
            .WithAll<PlayerAnimation>()
            .ForEach((SpriteRenderer link, ref HitData hitData) => {

                var spriteRenderer = link;
                var amountHealed = frameStats.totalHealed + heal + regenHealthAmount;

                if(amountHealed != 0) {
                    hitData.startTime = time;

                    hitData.hitColor = amountHealed > 0 ? new Color(0, 1, 0, 0) : new Color(1, 0, 0, 0);

                    var lifeLeech = 0;// enemiesHitThisFrame[0] * 1f;
                    Health.value = math.min(Health.value + amountHealed + lifeLeech, MaximumHealth.value);
                    
                    if(Health.value < 0) {
                        Health.value = 0;
                    }

                    frameStats.totalHealed = 0;
                    heal = 0;
                }

                PlayerHealthBar.fillAmount = Health.value / MaximumHealth.value;

                var target = hitData.hitColor;
                if(target != emptyColor) {
                    float f = (time - hitData.startTime) / hitData.duration;
                    spriteRenderer.color = Color.Lerp(spriteRenderer.color, target, f);
                    if(f >= 0.95f) {
                        hitData.hitColor = emptyColor;
                        hitData.startTime = 0;
                        spriteRenderer.color = hitData.hitColor;
                    }
                }
                requestHeal[0] = 0;
                stats[Stats.HealthID] = Health;
                chara.character.CharacterStats.stats = stats;
            }).WithoutBurst().Run();
            SystemAPI.SetSingleton(chara);
            SystemAPI.SetSingleton(frameStats);
    }
}