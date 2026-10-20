// ThiefDashSkill.cs
using Fusion;
using UnityEngine;

// 도적 스킬 "고속이동" (C키):
// 바라보는 방향으로 짧은 거리를 순식간에 순간이동한다. 벽에 막히면 벽 앞까지만 이동하고,
// 원래 있던 자리에는 아주 빠르게 지나간 듯한 속도선 파티클이 남는다.
public class ThiefDashSkill : CharacterSkillBase
{
    [Header("Dash (고속이동)")]
    [Tooltip("순간이동 거리 (유닛)")]
    [SerializeField] private float dashDistance = 3f;
    [Tooltip("이동을 막는 지형 레이어 (비워두면 Ground 레이어)")]
    [SerializeField] private LayerMask obstacleLayer;
    [Tooltip("벽 바로 앞에서 멈출 때 남기는 여유 거리")]
    [SerializeField] private float wallSkin = 0.05f;

    [Header("Trail Effect (출발한 자리의 잔상)")]
    [SerializeField] private Color trailColor = new Color(0.75f, 0.85f, 1f, 1f);
    [SerializeField] private int trailParticleCount = 14;

    // 연출을 모든 클라이언트에서 같은 위치에 그리기 위한 네트워크 값
    [Networked] private Vector2 DashFrom { get; set; }
    [Networked] private Vector2 DashTo { get; set; }

    private BoxCollider2D bodyCollider;

    private void Awake()
    {
        bodyCollider = GetComponent<BoxCollider2D>();

        if (obstacleLayer.value == 0)
            obstacleLayer = LayerMask.GetMask("Ground");
    }

    // 호스트에서만 실행: 플레이어 루트를 이동시킨다 (NetworkTransform이 모든 클라이언트에 동기화)
    protected override void Execute(float facingDirection)
    {
        PlayerData data = OwnerData;
        if (data == null) return;

        Transform playerRoot = data.transform;
        Vector2 from = playerRoot.position;
        Vector2 direction = new Vector2(Mathf.Sign(facingDirection), 0f);

        float distance = GetClearDistance(direction);
        Vector2 to = from + direction * distance;

        Teleport(playerRoot, to);

        DashFrom = from;
        DashTo = to;
    }

    // 지형에 막히면 그 앞까지만 이동
    private float GetClearDistance(Vector2 direction)
    {
        if (bodyCollider == null)
            return dashDistance;

        Bounds bounds = bodyCollider.bounds;

        RaycastHit2D hit = Physics2D.BoxCast(
            bounds.center,
            bounds.size,
            0f,
            direction,
            dashDistance,
            obstacleLayer
        );

        return hit.collider != null ? Mathf.Max(0f, hit.distance - wallSkin) : dashDistance;
    }

    private static void Teleport(Transform playerRoot, Vector2 position)
    {
        Rigidbody2D rb = playerRoot.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.position = position;

        playerRoot.position = position;

        // 보간 없이 즉시 이동한 것으로 동기화 (이동 경로가 미끄러지듯 보이지 않게)
        if (playerRoot.TryGetComponent(out NetworkTransform networkTransform))
            networkTransform.Teleport(position);
    }

    // 모든 클라이언트: 출발한 자리 → 도착한 자리 사이에 속도선 잔상
    protected override void OnSkillVisual()
    {
        RuntimeEffects.PlayTrail(
            DashFrom,
            DashTo,
            trailColor,
            new Color(trailColor.r, trailColor.g, trailColor.b, 0f),
            trailParticleCount,
            size: 0.12f,
            lifetime: 0.3f
        );
    }
}
