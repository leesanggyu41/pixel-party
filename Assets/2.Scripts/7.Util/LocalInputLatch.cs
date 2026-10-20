// LocalInputLatch.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class LocalInputLatch : MonoBehaviour
{
    public static bool JumpPending;
    public static bool AttackPending;
    public static bool SkillPending;          // 무기 스킬 (X)
    public static bool CharacterSkillPending; // 캐릭터 스킬 (C)

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame) JumpPending = true;
        if (Keyboard.current.zKey.wasPressedThisFrame) AttackPending = true;
        if (Keyboard.current.xKey.wasPressedThisFrame) SkillPending = true;
        if (Keyboard.current.cKey.wasPressedThisFrame) CharacterSkillPending = true;
    }
}