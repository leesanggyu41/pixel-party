using System.Collections.Generic;
using UnityEngine;

public class WeaponDatabase : MonoBehaviour
{
    public static WeaponDatabase Instance { get; private set; }

    [Header("등록된 무기 프리팹")]
    [SerializeField] private WeaponBase[] weapons;

    private readonly Dictionary<int, WeaponBase> weaponById = new Dictionary<int, WeaponBase>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        foreach (WeaponBase weapon in weapons)
        {
            if (weapon != null)
                weaponById[weapon.weaponId] = weapon;
        }
    }

    public WeaponBase GetWeapon(int weaponId)
    {
        if (weaponById.TryGetValue(weaponId, out WeaponBase weapon))
            return weapon;

        Debug.LogWarning($"무기 ID {weaponId}를 찾을 수 없습니다.");
        return null;
    }
    public WeaponBase[] GetAllWeapons()
    {
        return weapons;
    }
}