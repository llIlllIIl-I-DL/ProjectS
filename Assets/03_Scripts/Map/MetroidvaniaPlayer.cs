using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Metroidvania/Player")]
public class MetroidvaniaPlayer : MonoBehaviour
{
    // 획득한 열쇠 목록
    private HashSet<string> collectedKeys = new HashSet<string>();

    // 획득한 능력 목록
    private HashSet<string> unlockedAbilities = new HashSet<string>();

    // 열쇠 소지 여부 확인
    public bool HasKey(string keyId)
    {
        return collectedKeys.Contains(keyId);
    }

    // 능력 소지 여부 확인
    public bool HasAbility(string abilityId)
    {
        return unlockedAbilities.Contains(abilityId);
    }

    // 열쇠 획득
    public void CollectKey(string keyId)
    {
        collectedKeys.Add(keyId);
    }

    // 능력 획득
    public void UnlockAbility(string abilityId)
    {
        unlockedAbilities.Add(abilityId);
    }

    // 메시지 표시 (UI 연동 필요)
    public void ShowMessage(string message)
    {
        Debug.Log("Player Message: " + message);
        // 실제 UI 메시지 표시 로직 구현
    }

    // 방 전환 이펙트
    public void PlayRoomTransitionEffect()
    {
        // 화면 전환 이펙트 구현
    }
}