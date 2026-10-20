// ClassPromotionTable.cs
using System;
using Fusion;
using UnityEngine;

// 전직 설정 테이블.
// "전직 레벨에 도달했을 때 들고 있던 무기의 종류 → 어떤 캐릭터(아바타 프리팹)로 전직할지"를 인스펙터에서 지정한다.
// 새 직업이 생기면 Entries에 한 줄만 추가하면 된다.
[CreateAssetMenu(fileName = "ClassPromotionTable", menuName = "Pixel Party/Class Promotion Table")]
public class ClassPromotionTable : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("직업 이름 (표시용)")]
        public string className = "전사";

        [Tooltip("이 무기 종류를 들고 있으면 이 직업으로 전직")]
        public WeaponType weaponType = WeaponType.Sword;

        [Tooltip("전직 후 사용할 캐릭터(아바타) 프리팹")]
        public NetworkPrefabRef avatarPrefab;
    }

    [Tooltip("이 레벨에 도달하면 전직한다")]
    public int promotionLevel = 5;

    public Entry[] entries;

    // 무기 종류에 맞는 전직 정보를 찾는다. 프리팹이 아직 지정되지 않은 칸은 건너뛴다.
    public bool TryGetEntry(WeaponType weaponType, out Entry result)
    {
        if (entries != null)
        {
            foreach (Entry entry in entries)
            {
                if (entry != null && entry.weaponType == weaponType && entry.avatarPrefab.IsValid)
                {
                    result = entry;
                    return true;
                }
            }
        }

        result = null;
        return false;
    }
}
