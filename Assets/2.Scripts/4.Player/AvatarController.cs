// AvatarController.cs
using System.Collections;
using Fusion;
using UnityEngine;

public class AvatarController : NetworkBehaviour
{
    [SerializeField] private Transform avatarSlot; // AvatarSlot 연결
    [SerializeField] private NetworkPrefabRef startingAvatarPrefab; // vagabond 프리팹 연결

    [Header("전직")]
    [Tooltip("무기 종류별 전직 캐릭터 설정 (ClassPromotionTable 에셋)")]
    [SerializeField] private ClassPromotionTable promotionTable;

    [Networked] public NetworkId CurrentAvatarId { get; set; }
    [Networked] public NetworkBool HasPromoted { get; set; }

    [Networked, OnChangedRender(nameof(OnPromotionTriggered))]
    private int PromotionTick { get; set; }

    private NetworkObject currentAvatarObject;
    private Coroutine spawnCoroutine;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            SpawnAvatar(startingAvatarPrefab);
        }
    }

    // ===== 전직 =====

    // 전직 레벨 이상이고 아직 전직하지 않았다면, 들고 있는 무기 종류에 맞는 캐릭터로 전직 (호스트에서만)
    public void TryPromote()
    {
        if (!Object.HasStateAuthority || HasPromoted || promotionTable == null) return;

        if (!TryGetComponent(out PlayerData data) || data.Level < promotionTable.promotionLevel) return;
        if (!TryGetComponent(out WeaponController weaponController)) return;

        WeaponBase weapon = weaponController.CurrentWeapon;
        if (weapon == null) return;

        if (!promotionTable.TryGetEntry(weapon.weaponType, out ClassPromotionTable.Entry entry))
        {
            Debug.LogWarning($"[전직] {weapon.weaponType} 무기에 해당하는 전직 캐릭터가 ClassPromotionTable에 없습니다.");
            return;
        }

        HasPromoted = true;
        PromotionTick++;

        Debug.Log($"[전직] {entry.className} 로 전직 (무기: {weapon.weaponName})");

        // 아바타를 교체하면 무기가 붙어있던 손 위치도 사라지므로, 무기를 먼저 정리하고
        // 새 아바타가 준비되면 같은 무기를 다시 장착한다
        weaponController.PrepareAvatarSwap();
        SpawnAvatar(entry.avatarPrefab);
    }

    // 전직 연출 (모든 클라이언트): 금빛 기운이 퍼져나감
    private void OnPromotionTriggered()
    {
        RuntimeEffects.PlayBurst(
            transform.position,
            new Color(1f, 0.9f, 0.4f, 1f),
            new Color(1f, 0.7f, 0.1f, 0f),
            count: 50, speed: 5f, size: 0.15f, lifetime: 0.8f
        );
    }

    // ===== 아바타 소환 =====

    public void SpawnAvatar(NetworkPrefabRef avatarPrefab)
    {
        if (!Object.HasStateAuthority) return;

        if (currentAvatarObject != null)
        {
            Runner.Despawn(currentAvatarObject);
            currentAvatarObject = null;
        }

        // 아직 로드되지 않은 프리팹(처음 사용하는 전직 캐릭터)도 실패하지 않도록 비동기 소환 사용
        NetworkSpawnOp spawnOp = Runner.SpawnAsync(
            avatarPrefab,
            avatarSlot.position,
            avatarSlot.rotation,
            Object.InputAuthority
        );

        if (spawnOp.Status == NetworkSpawnStatus.Spawned)
        {
            OnAvatarSpawned(spawnOp.Object);
            return;
        }

        if (spawnOp.Status == NetworkSpawnStatus.Queued)
        {
            if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
            spawnCoroutine = StartCoroutine(WaitForAvatarSpawn(spawnOp));
            return;
        }

        Debug.LogError($"[아바타 생성 실패] SpawnAsync 상태: {spawnOp.Status}");
    }

    private IEnumerator WaitForAvatarSpawn(NetworkSpawnOp spawnOp)
    {
        while (spawnOp.Status == NetworkSpawnStatus.Queued)
            yield return null;

        spawnCoroutine = null;

        if (spawnOp.Status == NetworkSpawnStatus.Spawned)
            OnAvatarSpawned(spawnOp.Object);
        else
            Debug.LogError($"[아바타 생성 실패] SpawnAsync 상태: {spawnOp.Status}");
    }

    private void OnAvatarSpawned(NetworkObject newAvatar)
    {
        currentAvatarObject = newAvatar;
        CurrentAvatarId = newAvatar.Id; // [Networked] 변수 변경 -> 클라이언트에 자동 동기화됨
    }

    public override void Render()
    {
        // CurrentAvatarId가 유효하고, 현재 자식으로 안 들어간 상태라면 연동
        if (CurrentAvatarId.IsValid)
        {
            if (Runner.TryFindObject(CurrentAvatarId, out NetworkObject avatarObj))
            {
                if (avatarObj.transform.parent != transform)
                {
                    // 월드 값을 유지하며 붙이면, 플레이어가 오른쪽(localScale.x = -1)을 볼 때 전직하면
                    // 새 아바타의 로컬 스케일이 -1로 굳어 좌우가 반대로 보인다. 로컬 값 기준으로 붙인다.
                    avatarObj.transform.SetParent(transform, false);
                    avatarObj.transform.localPosition = avatarSlot.localPosition;
                    avatarObj.transform.localRotation = avatarSlot.localRotation;
                }
            }
        }
    }
}
