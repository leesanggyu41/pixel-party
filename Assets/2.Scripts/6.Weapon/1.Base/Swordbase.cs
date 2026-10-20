
using UnityEngine;
using Fusion;

public class Swordbase : WeaponBase
{
    [Header("Sword Specific Stats")]
    [SerializeField] private Vector2 HitSize = new Vector2(1f, 1f);
    [SerializeField] protected float HitboxOffset = 0.5f;
    [SerializeField] private LayerMask HitLayer;

    private static readonly Collider2D[] hitBuffer = new Collider2D[16];
    private ContactFilter2D hitFilter;
    private bool filterReady;

    protected override void PerformAttack(float facingDirection)
    {
        // 실제 공격 판정은 StateAuthority에서만 실행
        if (!Object.HasStateAuthority)
            return;

        if (weaponPoint == null)
        {
            Debug.LogWarning("[검 공격] weaponPoint가 연결되지 않았습니다.");
            return;
        }

        Vector2 hitboxCenter =
            (Vector2)weaponPoint.position
            + new Vector2(facingDirection * HitboxOffset, 0f);

        if (!filterReady)
        {
            hitFilter = new ContactFilter2D { useLayerMask = true, layerMask = HitLayer, useTriggers = true };
            filterReady = true;
        }

        int count = Physics2D.OverlapBox(hitboxCenter, HitSize, 0f, hitFilter, hitBuffer);

        float finalDamage = FinalDamage;

        for (int i = 0; i < count; i++)
        {
            IDamageable damageable = hitBuffer[i].GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(finalDamage);
        }
    }

    // Scene 뷰에 검 공격 범위 표시
    private void OnDrawGizmosSelected()
    {
        if (weaponPoint == null)
            return;

        // 현재 바라보는 방향을 기준으로 표시
        float facingDirection = transform.lossyScale.x >= 0 ? 1f : -1f;

        Vector2 hitboxCenter =
            (Vector2)weaponPoint.position
            + new Vector2(facingDirection * HitboxOffset, 0f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(hitboxCenter, HitSize);
    }
}
