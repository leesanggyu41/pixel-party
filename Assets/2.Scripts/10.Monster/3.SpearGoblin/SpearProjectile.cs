// SpearProjectile.cs
using UnityEngine;

// 창고블린이 던지는 창. 동작은 MonsterProjectileBase를 그대로 사용하고,
// 창만의 특수 효과(피격 이펙트 등)가 필요하면 OnHit을 확장한다.
public class SpearProjectile : MonsterProjectileBase
{
    protected override void OnHit(Collider2D other)
    {
        // TODO: 창 피격 이펙트/사운드
    }
}
