// MonsterProjectileBase.cs
using Fusion;
using UnityEngine;

// 몬스터가 던지는 투사체의 공통 기반 (창, 화살, 마법탄 등은 이 클래스를 상속)
// 이동/수명/피격 판정은 호스트(StateAuthority)에서만 처리하고, 위치는 NetworkTransform이 동기화한다.
public abstract class MonsterProjectileBase : NetworkBehaviour
{
    [Header("Hit Settings")]
    [Tooltip("피해를 줄 대상 레이어 (플레이어)")]
    [SerializeField] protected LayerMask targetLayer;
    [Tooltip("닿으면 사라지는 지형 레이어 (비워두면 지형을 무시)")]
    [SerializeField] protected LayerMask obstacleLayer;
    [Tooltip("true면 대상에게 맞아도 사라지지 않고 관통")]
    [SerializeField] protected bool piercing = false;

    [Header("Motion")]
    [Tooltip("0이면 직선 비행, 값이 있으면 포물선으로 떨어진다")]
    [SerializeField] protected float gravity = 0f;
    [Tooltip("true면 이동 방향으로 스프라이트를 회전")]
    [SerializeField] protected bool rotateToVelocity = true;

    [Networked] protected Vector2 Velocity { get; set; }
    [Networked] protected float Damage { get; set; }
    [Networked] private TickTimer LifeTimer { get; set; }

    // Runner.Spawn의 onBeforeSpawned 콜백에서 호출해야 첫 동기화부터 값이 반영된다
    public void Initialize(Vector2 direction, float speed, float damage, float lifetime)
    {
        Velocity = direction.normalized * speed;
        Damage = damage;
        LifeTimer = TickTimer.CreateFromSeconds(Runner, lifetime);

        if (rotateToVelocity)
            ApplyRotation();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (LifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        if (gravity != 0f)
            Velocity += Vector2.down * gravity * Runner.DeltaTime;

        transform.position += (Vector3)(Velocity * Runner.DeltaTime);

        if (rotateToVelocity)
            ApplyRotation();
    }

    private void ApplyRotation()
    {
        if (Velocity.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(Velocity.y, Velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority) return;

        int otherLayerBit = 1 << other.gameObject.layer;

        if ((targetLayer.value & otherLayerBit) != 0)
        {
            IDamageable damageable = other.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(Damage);
                OnHit(other);

                if (!piercing)
                    Runner.Despawn(Object);
            }

            return;
        }

        if ((obstacleLayer.value & otherLayerBit) != 0)
        {
            OnHit(other);
            Runner.Despawn(Object);
        }
    }

    // 파생 클래스에서 피격 연출/추가 효과가 필요할 때 재정의
    protected virtual void OnHit(Collider2D other) { }
}
