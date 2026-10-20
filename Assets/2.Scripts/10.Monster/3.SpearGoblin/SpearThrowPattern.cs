// SpearThrowPattern.cs
using Fusion;
using UnityEngine;

// 창고블린의 원거리 공격: WindUp(1프레임) → 창 투척 + Attack(1프레임) → 종료
// 두 모션 모두 1프레임이라 애니메이터 전환 시간 대신 코드의 지속시간으로 길이를 정한다.
//
// 애니메이터 설정: int 파라미터 "AttackPhase" (0=평소, 1=WindUp, 2=Attack)
//   Any State → SpearWindUp : AttackPhase == 1
//   Any State → SpearAttack : AttackPhase == 2
//   SpearWindUp/SpearAttack → Idle(Move) : AttackPhase == 0   (Has Exit Time 끄기)
public class SpearThrowPattern : MonsterPatternBase
{
    [Header("Timing")]
    [SerializeField] private float windupDuration = 0.4f;     // WindUp 포즈 유지 시간
    [SerializeField] private float attackPoseDuration = 0.3f; // 던진 뒤 Attack 포즈 유지 시간

    [Header("Projectile")]
    [SerializeField] private NetworkPrefabRef spearPrefab;
    [SerializeField] private Transform throwPoint;            // 창이 생성되는 위치 (없으면 몬스터 위치)
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float damage = 6f;
    [SerializeField] private float projectileLifetime = 3f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string phaseParameter = "AttackPhase";

    private const int PhaseNone = 0;
    private const int PhaseWindup = 1;
    private const int PhaseAttack = 2;

    private int phaseHash;

    // 모든 클라이언트가 같은 애니메이션을 보도록 단계만 네트워크로 동기화
    [Networked, OnChangedRender(nameof(OnAnimPhaseChanged))]
    private int AnimPhase { get; set; }

    // 아래는 호스트에서만 쓰는 값
    private int step; // 0=대기, 1=WindUp 중, 2=Attack 중
    private TickTimer stepTimer;
    private Transform target;

    private void Awake()
    {
        phaseHash = Animator.StringToHash(phaseParameter);
    }

    public override void TryStart(Transform newTarget)
    {
        if (!Object.HasStateAuthority) return;
        if (IsOnCooldown || step != 0) return;

        target = newTarget;
        step = 1;
        stepTimer = TickTimer.CreateFromSeconds(Runner, windupDuration);
        AnimPhase = PhaseWindup;
    }

    // MonsterBase가 매 틱 호출. 패턴이 끝나면 true
    public override bool TickPattern()
    {
        if (!Object.HasStateAuthority) return step == 0;

        if (step == 1 && stepTimer.Expired(Runner))
        {
            ThrowSpear();

            step = 2;
            stepTimer = TickTimer.CreateFromSeconds(Runner, attackPoseDuration);
            AnimPhase = PhaseAttack;
        }
        else if (step == 2 && stepTimer.Expired(Runner))
        {
            step = 0;
            stepTimer = TickTimer.None;
            target = null;
            CooldownTimer = TickTimer.CreateFromSeconds(Runner, cooldown);
            AnimPhase = PhaseNone;

            return true;
        }

        return false;
    }

    private void ThrowSpear()
    {
        if (!spearPrefab.IsValid)
        {
            Debug.LogWarning($"[{name}] spearPrefab이 설정되지 않았습니다.");
            return;
        }

        Vector2 origin = throwPoint != null ? throwPoint.position : transform.position;

        // 던지는 순간 타깃의 위치를 향해 발사 (타깃이 사라졌다면 바라보는 방향으로)
        Vector2 direction = target != null
            ? (Vector2)target.position - origin
            : Vector2.right * -Mathf.Sign(transform.lossyScale.x);

        float spearDamage = damage;
        float speed = projectileSpeed;
        float lifetime = projectileLifetime;

        Runner.Spawn(
            spearPrefab,
            origin,
            Quaternion.identity,
            Object.InputAuthority,
            (runner, spawned) =>
            {
                spawned.GetComponent<MonsterProjectileBase>()
                       .Initialize(direction, speed, spearDamage, lifetime);
            }
        );
    }

    private void OnAnimPhaseChanged()
    {
        if (animator != null)
            animator.SetInteger(phaseHash, AnimPhase);
    }
}
