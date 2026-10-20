using UnityEngine;
using Fusion;

public class Projectile : NetworkBehaviour
{
    [Networked] private Vector2 Direction { get; set; }
    [Networked] private float Speed { get; set; }
    [Networked] private float Damage { get; set; }

    [Header("Hit Settings")]
    [SerializeField] private LayerMask HitLayer;

    public void Initialize(
        Vector2 direction,
        float speed,
        float damage)
    {
        if (!HasStateAuthority) return;

        Direction = direction.normalized;
        Speed = speed;
        Damage = damage;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        transform.position +=
            (Vector3)(Direction * Speed * Runner.DeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasStateAuthority)
            return;

        // HitLayer에 포함되지 않은 대상은 무시
        if ((HitLayer.value & (1 << other.gameObject.layer)) == 0)
            return;

        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(Damage);

            Runner.Despawn(Object);
        }
    }
}