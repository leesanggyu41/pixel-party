using UnityEngine;
using Fusion;

public enum WeaponType
{
    Sword,
    Bow,
    Dagger,
    Staff
}

public abstract class WeaponBase : NetworkBehaviour
{
    [Networked] public PlayerRef Owner { get; set; }

    public string weaponName = "Weapon";
    [Header("Weapon Stats")]
    public float damage = 10f;
    public float attackDelay = 1f;
    public WeaponType weaponType;

    public abstract void Attack();

}
