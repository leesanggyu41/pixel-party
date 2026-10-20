// RuntimeEffects.cs
using UnityEngine;

// 스프라이트/프리팹 없이 코드로 만드는 간단한 파티클 이펙트.
// 도트 그래픽에 어울리는 네모난 파티클을 쓰며, 나중에 전용 이펙트 프리팹으로 교체해도 된다.
public static class RuntimeEffects
{
    private static Material particleMaterial;

    private static Material ParticleMaterial
    {
        get
        {
            if (particleMaterial == null)
                particleMaterial = new Material(Shader.Find("Sprites/Default"));

            return particleMaterial;
        }
    }

    // 애니메이션이 있는 이펙트 프리팹(SpriteRenderer + Animator)을 한 번 재생하고, 애니메이션이 끝나면 바로 제거한다.
    // 로컬에서만 생성되는 연출용이라 네트워크 오브젝트가 아니다 (모든 클라이언트가 각자 호출)
    public static void PlayAnimatedEffect(GameObject effectPrefab, Vector3 position, bool flipX)
    {
        if (effectPrefab == null) return;

        GameObject effect = Object.Instantiate(effectPrefab, position, Quaternion.identity);

        SpriteRenderer spriteRenderer = effect.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.flipX = flipX;

        // 애니메이션 길이만큼만 보여주고 제거
        float duration = 0.5f;
        Animator animator = effect.GetComponentInChildren<Animator>();

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            float longest = 0f;
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                longest = Mathf.Max(longest, clip.length);

            if (longest > 0f)
                duration = longest / Mathf.Max(0.01f, animator.speed);
        }

        Object.Destroy(effect, duration);
    }

    // 두 지점 사이에 빠르게 지나간 듯한 속도선 이펙트 (순간이동/돌진한 자리의 잔상)
    // from → to 선을 따라 속도선 파티클이 생기고, 이동 반대 방향으로 짧게 흩어지며 사라진다
    public static void PlayTrail(Vector3 from, Vector3 to, Color startColor, Color endColor,
                                 int count, float size, float lifetime)
    {
        ParticleSystem particles = CreateSystem("TrailEffect", null, from,
                                                startColor, endColor, 0f, size, lifetime, loop: false);

        var emission = particles.emission;
        emission.rateOverTime = 0f;

        var shape = particles.shape;
        shape.enabled = false;

        // 이동 방향으로 길게 늘어난 속도선으로 보이게 함
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 6f;
        renderer.velocityScale = 0.1f;

        Vector3 line = to - from;
        Vector3 direction = line.sqrMagnitude > 0.0001f ? line.normalized : Vector3.right;
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f);

        particles.Play();

        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? i / (float)(count - 1) : 0f;

            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = from + line * t + perpendicular * Random.Range(-0.25f, 0.25f),
                velocity = -direction * Random.Range(1f, 4f), // 이동한 반대쪽으로 흩어짐
                startLifetime = lifetime * Random.Range(0.7f, 1f),
                startSize = size * Random.Range(0.6f, 1.2f),
                startColor = startColor
            };

            particles.Emit(emitParams, 1);
        }

        Object.Destroy(particles.gameObject, lifetime + 0.5f);
    }

    // 한 번 터지고 사라지는 이펙트 (원형으로 퍼짐)
    public static void PlayBurst(Vector3 position, Color startColor, Color endColor,
                                 int count, float speed, float size, float lifetime)
    {
        ParticleSystem particles = CreateSystem("BurstEffect", null, position,
                                                startColor, endColor, speed, size, lifetime, loop: false);

        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;

        particles.Play();
        Object.Destroy(particles.gameObject, lifetime + 0.5f);
    }

    // 계속 이어지는 이펙트 (아래에서 위로 솟는 불꽃/기운). 반환된 시스템을 Play/Stop으로 제어
    public static ParticleSystem CreateAura(Transform parent, Vector3 localPosition, Color startColor, Color endColor,
                                            float emissionRate, float speed, float size, float lifetime, float radius)
    {
        ParticleSystem particles = CreateSystem("AuraEffect", parent, localPosition,
                                                startColor, endColor, speed, size, lifetime, loop: true);

        var emission = particles.emission;
        emission.rateOverTime = emissionRate;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;

        // 위로 솟구치는 느낌
        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(speed);

        particles.Stop();
        return particles;
    }

    private static ParticleSystem CreateSystem(string name, Transform parent, Vector3 position,
                                               Color startColor, Color endColor,
                                               float speed, float size, float lifetime, bool loop)
    {
        GameObject effectObject = new GameObject(name);

        if (parent != null)
        {
            effectObject.transform.SetParent(parent, false);
            effectObject.transform.localPosition = position;
        }
        else
        {
            effectObject.transform.position = position;
        }

        ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particles.main;
        main.loop = loop;
        main.playOnAwake = false;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = startColor;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(endColor, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var renderer = effectObject.GetComponent<ParticleSystemRenderer>();
        renderer.material = ParticleMaterial;
        renderer.sortingOrder = 40;

        return particles;
    }
}
