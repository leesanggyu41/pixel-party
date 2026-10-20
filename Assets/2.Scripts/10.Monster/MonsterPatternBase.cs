// MonsterPatternBase.cs
using Fusion;
using UnityEngine;

public abstract class MonsterPatternBase : NetworkBehaviour
{
    [Header("Common")]
    public float cooldown = 3f;
    public float triggerRange = 2f; // 이 거리 안이어야 패턴 발동 가능

    [Networked] protected TickTimer CooldownTimer { get; set; }

    public bool IsOnCooldown => !CooldownTimer.ExpiredOrNotRunning(Runner);
    public float TriggerRange => triggerRange;

    // 호스트에서만 호출 - 패턴 시작
    public abstract void TryStart(Transform target);

    // 매 틱 호출, 패턴이 끝났으면 true 반환 (Idle 상태로 복귀했다는 뜻)
    public abstract bool TickPattern();
}