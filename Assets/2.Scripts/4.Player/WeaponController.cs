using System.Collections;
using Fusion;
using UnityEngine;

public class WeaponController : NetworkBehaviour
{
    private Transform weaponPoint;
    private Coroutine spawnCoroutine;

    [SerializeField] private WeaponBase startingWeapon;

    [Networked] public NetworkId networkId { get; set; }
    private WeaponBase currentWeapon;

    // 호스트 전용: 마지막으로 장착한 무기 프리팹, 그리고 아바타 교체(전직) 후 다시 장착할 무기
    private WeaponBase equippedPrefab;
    private WeaponBase pendingPrefab;

    public WeaponBase CurrentWeapon => currentWeapon;

    public void SetWeaponPoint(Transform point) => weaponPoint = point;

    public void TryEquipStartingWeapon()
    {
        if (!Object.HasStateAuthority) return;

        // 전직으로 아바타가 바뀐 경우: 시작 무기가 아니라 들고 있던 무기를 새 손 위치에 다시 장착
        if (pendingPrefab != null)
        {
            WeaponBase weapon = pendingPrefab;
            pendingPrefab = null;
            EquipWeapon(weapon);
            return;
        }

        if (startingWeapon != null)
        {
            EquipWeapon(startingWeapon);
        }
    }

    // 아바타 교체(전직) 직전에 호출 (호스트): 옛 손 위치에 붙은 무기를 먼저 정리하고, 새 아바타 준비 후 다시 장착한다
    public void PrepareAvatarSwap()
    {
        if (!Object.HasStateAuthority) return;

        pendingPrefab = equippedPrefab;

        if (currentWeapon != null)
        {
            Runner.Despawn(currentWeapon.Object);
            currentWeapon = null;
        }

        networkId = default;
        weaponPoint = null; // 새 아바타가 Init에서 새 손 위치를 지정할 때까지 대기
    }

    public override void FixedUpdateNetwork()
    {
        if (weaponPoint == null) return;
        if (!Object.HasStateAuthority && !Object.HasInputAuthority) return; // 둘 중 하나면 통과

        if (GetInput(out PlayerNetworkInput input))
        {
            if (input.AttackInput && currentWeapon != null)
            {
                float facingDirection = Mathf.Sign(-transform.localScale.x);
                currentWeapon.Attack(facingDirection);
            }

            if (input.SkillInput && currentWeapon != null)
            {
                float facingDirection = Mathf.Sign(-transform.localScale.x);
                currentWeapon.UseSkill(facingDirection);
            }
        }
    }


    public void EquipWeapon(WeaponBase newWeapon)
    {
        if (!Object.HasStateAuthority || weaponPoint == null)
        {
            Debug.LogWarning(
                $"장착 중단: StateAuthority={Object.HasStateAuthority}, " +
                $"weaponPoint={(weaponPoint == null ? "null" : weaponPoint.name)}"
            );
            return;
        }

        if (currentWeapon != null)
        {
            Runner.Despawn(currentWeapon.Object);
            currentWeapon = null;
        }

        equippedPrefab = newWeapon;

        // 프리팹이 아직 로드되지 않았어도(방 생성 직후의 시작 무기) 실패하지 않도록 비동기 소환 사용
        NetworkSpawnOp spawnOp = Runner.SpawnAsync(
            newWeapon.gameObject,
            weaponPoint.position,
            Quaternion.identity, // 프리팹의 Y축 180° 회전이 네트워크 상태로 동기화되지 않도록 회전 없이 소환
            Object.InputAuthority
        );

        if (spawnOp.Status == NetworkSpawnStatus.Spawned)
        {
            OnWeaponSpawned(spawnOp.Object);
            return;
        }

        if (spawnOp.Status == NetworkSpawnStatus.Queued)
        {
            if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
            spawnCoroutine = StartCoroutine(WaitForWeaponSpawn(spawnOp));
            return;
        }

        Debug.LogError($"[무기 생성 실패] SpawnAsync 상태: {spawnOp.Status}");
    }

    private IEnumerator WaitForWeaponSpawn(NetworkSpawnOp spawnOp)
    {
        while (spawnOp.Status == NetworkSpawnStatus.Queued)
            yield return null;

        spawnCoroutine = null;

        if (spawnOp.Status == NetworkSpawnStatus.Spawned)
            OnWeaponSpawned(spawnOp.Object);
        else
            Debug.LogError($"[무기 생성 실패] SpawnAsync 상태: {spawnOp.Status}");
    }

    private void OnWeaponSpawned(NetworkObject weaponObj)
    {
        // 월드 값을 유지하며 붙이면(기본값) 장착 순간 플레이어가 보던 방향이 무기에 굳어버린다.
        // 로컬 값 기준으로 붙여야 플레이어의 좌우 반전(localScale)이 무기에도 그대로 적용된다.
        AttachToWeaponPoint(weaponObj.transform);

        currentWeapon = weaponObj.GetComponent<WeaponBase>();
        currentWeapon.Owner = Object.InputAuthority;
        networkId = weaponObj.Id;
    }


    public override void Render()
    {
        if (!networkId.IsValid)
        {
            // 무기가 없어진 상태(전직으로 교체 중 등): 이미 Despawn된 무기 참조를 남겨두지 않는다
            currentWeapon = null;
            return;
        }

        // 네트워크 ID가 바뀌었거나 참조가 사라졌다면 새 무기를 찾아 연결 (호스트 포함 모든 피어)
        if (currentWeapon == null ||
            currentWeapon.Object == null ||
            currentWeapon.Object.Id != networkId)
        {
            if (!Runner.TryFindObject(networkId, out NetworkObject weaponObj))
                return;

            currentWeapon = weaponObj.GetComponent<WeaponBase>();
        }

        // 무기의 NetworkTransform(SyncParent)이 호스트에서 부모를 바꿔버려도
        // 매 프레임 WeaponPoint 자식으로 되돌려 놓는다
        if (currentWeapon != null && weaponPoint != null &&
            currentWeapon.transform.parent != weaponPoint)
        {
            AttachToWeaponPoint(currentWeapon.transform);
        }
    }

    // 무기를 WeaponPoint의 자식으로 붙이고 로컬 위치/회전을 초기화 (호스트/클라이언트 공통)
    private void AttachToWeaponPoint(Transform weapon)
    {
        weapon.SetParent(weaponPoint, false);
        weapon.localPosition = Vector3.zero;

        // 무기 스프라이트와 플레이어 스프라이트는 모두 기본이 왼쪽을 본다.
        // 로컬 회전을 0으로 두면 플레이어의 좌우 반전(localScale)을 그대로 따라 항상 같은 방향을 본다.
        // (프리팹 루트에 설정된 Y축 180° 회전을 유지하면 플레이어와 항상 반대로 보인다)
        weapon.localRotation = Quaternion.identity;

        // 플레이어가 오른쪽(localScale.x = -1)을 볼 때 부모가 바뀌면 월드 스케일을 유지하느라
        // 무기의 로컬 스케일이 -1로 굳어버려 방향이 반대로 보인다. 항상 프리팹 스케일로 되돌린다.
        WeaponBase weaponBase = weapon.GetComponent<WeaponBase>();
        weapon.localScale = weaponBase != null ? weaponBase.HeldLocalScale : Vector3.one;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestEquipRewardWeapon(NetworkId weaponId)
    {
        if (!Object.HasStateAuthority)
            return;

        if (!Runner.TryFindObject(weaponId, out NetworkObject weaponObject))
            return;

        WeaponBase weapon = weaponObject.GetComponent<WeaponBase>();

        if (weapon == null)
            return;

        EquipWeapon(weapon);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestEquipRewardWeapon(int weaponId)
    {
        if (!Object.HasStateAuthority)
            return;

        if (WeaponDatabase.Instance == null)
        {
            Debug.LogError("WeaponDatabase를 찾을 수 없습니다.");
            return;
        }

        WeaponBase weapon = WeaponDatabase.Instance.GetWeapon(weaponId);

        if (weapon == null)
        {
            Debug.LogWarning($"무기 ID {weaponId}를 찾을 수 없습니다.");
            return;
        }

        EquipWeapon(weapon);

        Debug.Log($"무기 보상 장착 완료: {weapon.weaponName}");
    }
}