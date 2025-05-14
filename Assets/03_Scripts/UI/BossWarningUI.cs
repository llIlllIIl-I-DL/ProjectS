using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

// UI 기반을 싱글톤
public class BossWarningUI : Singleton<BossWarningUI>
{
    [Header("창")]
    [SerializeField] public GameObject bossWarningUI;
    [SerializeField] public Transform bossWarningUIParents;

    [Header("선택 버튼")]
    [SerializeField] public Button[] Btn;
    //[SerializeField] public Button[] noBtn;

    [HideInInspector] GameObject _bossWarningUI;
    [HideInInspector] public bool isApproved;

    private ObjectDoor currentDoor;

    GameObject _interactor;

    public void BossWarningWindowUI(GameObject interactor, ObjectDoor door)
    {
        _bossWarningUI = Instantiate(bossWarningUI, bossWarningUIParents);
        _interactor = interactor;
        currentDoor = door;

        Btn = _bossWarningUI.GetComponentsInChildren<Button>();

        Btn[0].onClick.AddListener(() => YesYesYes());
        Btn[1].onClick.AddListener(() => NoNoNo());

        Time.timeScale = 0f;
    }
    
    public void YesYesYes()
    {
        isApproved = true;

        DestroyUI(isApproved);
    }

    public void NoNoNo()
    {
        isApproved = false;

        DestroyUI(isApproved);
    }

    public void DestroyUI(bool isApproved)
    {
        Destroy(_bossWarningUI);
        Time.timeScale = 1f;

        if (currentDoor != null)
        {
            currentDoor.OnEntrance(isApproved);
        }
        else
        {
            Debug.Log("상호작용할 대상이 설정되지 않았습니다.");
        }
    }
}
