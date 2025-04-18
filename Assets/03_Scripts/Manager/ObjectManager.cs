using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 상호작용 오브젝트를 관리하는 매니저
/// </summary>
public class ObjectManager : Singleton<ObjectManager>
{
    // 이벤트 정의
    public event Action<string, bool> OnTriggerStateChanged;
    public event Action<string, bool> OnDoorStateChanged;

    // 등록된 오브젝트 목록
    private Dictionary<string, BaseObject> registeredObjects = new Dictionary<string, BaseObject>();

    // 트리거 그룹 (여러 트리거로 하나의 반응을 제어)
    [System.Serializable]
    public class TriggerGroup
    {
        public string groupId;
        public List<string> triggerIds = new List<string>();
        public int requiredCount = 0; // 0이면 모두 필요
        public bool sequential = false; // 순서가 중요한지 여부
        public List<string> targetObjectIds = new List<string>(); // 영향 받는 오브젝트들
        public string actionType = "toggle"; // toggle, open, close, activate, deactivate 등
    }

    [SerializeField] private List<TriggerGroup> triggerGroups = new List<TriggerGroup>();

    // 각 그룹별 활성화된 트리거 추적
    private Dictionary<string, List<string>> activeTriggers = new Dictionary<string, List<string>>();
    private Dictionary<string, List<string>> triggerSequence = new Dictionary<string, List<string>>();

    protected override void Awake()
    {
        base.Awake();
        foreach (var group in triggerGroups)
        {
            activeTriggers[group.groupId] = new List<string>();
            triggerSequence[group.groupId] = new List<string>();
        }
    }

    #region Registration Methods

    /// <summary>
    /// 오브젝트 등록
    /// </summary>
    public void RegisterObject(string objectId, BaseObject obj)
    {
        if (!registeredObjects.ContainsKey(objectId))
        {
            registeredObjects[objectId] = obj;
            Debug.Log($"오브젝트 등록: {objectId}");
        }
    }

    /// <summary>
    /// 트리거 상태 변경 등록
    /// </summary>
    public void RegisterTriggerState(string triggerId, bool isActive)
    {
        // 이벤트 알림
        OnTriggerStateChanged?.Invoke(triggerId, isActive);

        // 그룹별 트리거 상태 업데이트
        UpdateTriggerGroups(triggerId, isActive);

        Debug.Log($"트리거 상태 변경: {triggerId}, 활성화: {isActive}");
    }

    #endregion

    #region Trigger Group Management

    /// <summary>
    /// 모든 트리거 그룹 상태 업데이트
    /// </summary>
    private void UpdateTriggerGroups(string triggerId, bool isActive)
    {
        foreach (var group in triggerGroups)
        {
            if (group.triggerIds.Contains(triggerId))
            {
                UpdateGroupState(group, triggerId, isActive);
            }
        }
    }

    /// <summary>
    /// 특정 그룹 상태 업데이트
    /// </summary>
    private void UpdateGroupState(TriggerGroup group, string triggerId, bool isActive)
    {
        // 기존 코드 유지
        if (isActive)
        {
            if (!activeTriggers[group.groupId].Contains(triggerId))
            {
                activeTriggers[group.groupId].Add(triggerId);

                // 시퀀스 추적
                if (group.sequential && !triggerSequence[group.groupId].Contains(triggerId))
                {
                    triggerSequence[group.groupId].Add(triggerId);
                }
            }
        }
        else
        {
            activeTriggers[group.groupId].Remove(triggerId);

            // 시퀀스가 깨졌으면 리셋
            if (group.sequential)
            {
                triggerSequence[group.groupId].Clear();
            }
        }

        // 그룹 조건 확인
        CheckGroupCondition(group);
    }

    /// <summary>
    /// 그룹 조건 만족 여부 확인 및 액션 실행
    /// </summary>
    private void CheckGroupCondition(TriggerGroup group)
    {
        bool conditionMet = false;

        // 필요한 개수 확인
        int requiredCount = group.requiredCount == 0 ? group.triggerIds.Count : group.requiredCount;

        if (group.sequential)
        {
            // 순서가 중요한 경우 - 시퀀스 확인
            if (triggerSequence[group.groupId].Count == requiredCount)
            {
                conditionMet = true;
                for (int i = 0; i < requiredCount; i++)
                {
                    // 정해진 순서와 달라지면 조건 실패
                    if (i >= triggerSequence[group.groupId].Count ||
                        triggerSequence[group.groupId][i] != group.triggerIds[i])
                    {
                        conditionMet = false;
                        break;
                    }
                }
            }
        }
        else
        {
            // 순서가 중요하지 않은 경우 - 개수만 확인
            conditionMet = activeTriggers[group.groupId].Count >= requiredCount;
        }

        // 조건 충족 시 액션 실행
        if (conditionMet)
        {
            ExecuteAction(group);
        }
        else
        {
            // 조건 해제 시 반대 액션 실행 가능
            ExecuteReverseAction(group);
        }
    }

    #endregion

    #region Actions

    /// <summary>
    /// 그룹 조건 충족 시 액션 실행
    /// </summary>
    private void ExecuteAction(TriggerGroup group)
    {
        foreach (string targetId in group.targetObjectIds)
        {
            if (registeredObjects.TryGetValue(targetId, out BaseObject targetObject))
            {
                // 타입에 따른 액션 실행
                if (targetObject is ObjectDoor door)
                {
                    switch (group.actionType.ToLower())
                    {
                        case "open":
                            door.Unlock();
                            door.OpenDoor();
                            break;
                        case "close":
                            door.CloseDoor();
                            break;
                        case "toggle":
                            door.Unlock();
                            door.OpenDoor();
                            break;
                        case "lock":
                            door.ToggleLock(true);
                            break;
                        case "unlock":
                            door.Unlock();
                            break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 그룹 조건 해제 시 반대 액션 실행
    /// </summary>
    private void ExecuteReverseAction(TriggerGroup group)
    {
        // 특정 액션만 반대 동작 실행 (필요에 따라)
        if (group.actionType == "toggle")
        {
            foreach (string targetId in group.targetObjectIds)
            {
                if (registeredObjects.TryGetValue(targetId, out BaseObject targetObject))
                {
                    if (targetObject is ObjectDoor door)
                    {
                        door.CloseDoor();
                    }

                    // 다른 오브젝트 타입들에 대한 처리도 추가 가능
                }
            }
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// 특정 오브젝트 찾기
    /// </summary>
    public BaseObject GetObject(string objectId)
    {
        if (registeredObjects.TryGetValue(objectId, out BaseObject obj))
        {
            return obj;
        }
        return null;
    }

    /// <summary>
    /// 모든 트리거 상태 초기화
    /// </summary>
    public void ResetAllTriggers()
    {
        foreach (var group in triggerGroups)
        {
            activeTriggers[group.groupId].Clear();
            triggerSequence[group.groupId].Clear();
        }
    }

    #endregion
}