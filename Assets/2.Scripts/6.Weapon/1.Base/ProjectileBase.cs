using UnityEngine;
using Fusion;

public abstract class ProjectileBase : WeaponBase
{
    [Header("Projectile Settings")]
    [SerializeField] protected NetworkPrefabRef projectilePrefab;
    [SerializeField] protected float projectileSpeed = 10f;
    [SerializeField] protected Transform projectilePoint;
    [Tooltip("투사체 프리팹의 스프라이트가 기본적으로 오른쪽을 보고 있으면 체크. 발사 방향과 반대로 보이면 이 값을 바꾼다")]
    [SerializeField] protected bool projectileSpriteFacesRight = true;

    protected override void PerformAttack(float facingDirection)
    {
        if (!Object.HasInputAuthority)
            return;

        RPC_RequestProjectile(facingDirection);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestProjectile(float facingDirection)
    {
        SpawnProjectile(facingDirection);
    }

    private void SpawnProjectile(float facingDirection)
    {
        if (!HasStateAuthority)
            return;

        if (!projectilePrefab.IsValid)
        {
            Debug.LogWarning($"[{gameObject.name}] Projectile Prefab이 설정되지 않았습니다.");
            return;
        }

        Vector2 spawnPosition = projectilePoint != null
            ? projectilePoint.position
            : weaponPoint.position;

        // Z축 180° 회전은 스프라이트가 거꾸로 뒤집혀 보이므로, 좌우만 뒤집는 Y축 180° 회전을 사용한다.
        // 프리팹 스프라이트가 오른쪽을 보고 있으면 오른쪽 발사 시 회전 없음, 왼쪽을 보고 있으면 반대로 적용.
        bool facingRight = facingDirection > 0;
        Quaternion spawnRotation = facingRight == projectileSpriteFacesRight
            ? Quaternion.identity
            : Quaternion.Euler(0f, 180f, 0f);

        NetworkObject projectileObject = Runner.Spawn(
            projectilePrefab,
            spawnPosition,
            spawnRotation,
            Object.InputAuthority
        );

        Projectile projectile = projectileObject.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.Initialize(
                new Vector2(facingDirection, 0f),
                projectileSpeed,
                FinalDamage
            );
        }
    }
}