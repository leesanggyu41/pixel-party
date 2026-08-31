using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [SerializeField] private Transform handSocket; // 무기가 붙을 손 위치
    //private WeaponInstance currentWeaponInstance;
    private float attackCooldownTimer;
    private float currentCooldown;

    // private void Update()
    // {
    //     attackCooldownTimer -= Time.deltaTime;

    //     if (Keyboard.current == null || currentWeaponInstance == null) return;

    //     if (Keyboard.current.zKey.wasPressedThisFrame && attackCooldownTimer <= 0f)
    //     {
    //         float facingDirection = Mathf.Sign(transform.localScale.x);
    //         currentWeaponInstance.Attack(facingDirection); // 명령만 내림, 나머진 무기가 알아서
    //         attackCooldownTimer = currentCooldown;
    //     }
    // }

    // 보상으로 무기 선택했을 때 호출
    // public void EquipWeapon(WeaponData newWeaponData)
    // {
    //     if (currentWeaponInstance != null)
    //     {
    //         Destroy(currentWeaponInstance.gameObject);
    //     }

    //     GameObject weaponObj = Instantiate(newWeaponData.weaponPrefab, handSocket);
    //     weaponObj.transform.localPosition = Vector3.zero;

    //     currentWeaponInstance = weaponObj.GetComponent<WeaponInstance>();
    //     currentWeaponInstance.Setup(gameObject);

    //     currentCooldown = newWeaponData.attackCooldown;
    //     attackCooldownTimer = 0f;
    // }
}