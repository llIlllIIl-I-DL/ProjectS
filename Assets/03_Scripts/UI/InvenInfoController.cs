using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using Unity.VisualScripting;

public class InvenInfoController : MonoBehaviour
{
    public static InvenInfoController Instance { get; private set; }

    static PlayerHP playerHP;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("아이템 설명과 장착/해제 버튼")]
    [SerializeField] private TextMeshProUGUI descriptionTitle;
    [SerializeField] private TextMeshProUGUI itemDescription;

    [SerializeField] private Button utilityEquipBtn;
    [SerializeField] private Button utilityRemoveBtn;

    [Header("현재 장착 중인 특성")]
    [SerializeField] private List<Image> currentEquippedUtility;

    public void Start()
    {
        playerHP = FindObjectOfType<PlayerHP>();

        //slotInteractBtn.onClick.AddListener(() => slotInteract());
    }

    public void slotInteract(string ItemDescription, string ItemName, Sprite ItemIcon, float effectValue, AttributeType attributeType, int id)
    {
        descriptionTitle.text = ItemName;
        itemDescription.text = ItemDescription;

        utilityEquipBtn.onClick.RemoveAllListeners();
        utilityEquipBtn.onClick.AddListener(() => UtilityEquipped(ItemIcon, effectValue, attributeType, id));
    }

    public void UtilityEquipped(Sprite ItemIcon, float effectValue, AttributeType attributeType, int id)
    {
        //플레이어 쪽의 현재 장착 중인 특성 아이콘 업데이트
        for (int i = 0; i < currentEquippedUtility.Count; i++)
        {
            if (currentEquippedUtility[i].sprite == null)
            {
                Color temp = currentEquippedUtility[i].color;
                temp.a = 1f;
                currentEquippedUtility[i].color = temp;

                currentEquippedUtility[i].sprite = ItemIcon;
            }

            switch (id)
            {
                case 1001:

                    playerHP.IncreaseMaxHP(effectValue);
                    PlayerUI.Instance.UpdatePlayerHPInUItext();
                    break;


                case 1002:

                    Debug.Log("저는 1002번입니다");
                    break;

                case 1003:

                    Debug.Log("저는 1003번입니다");
                    break;

                case 1004:

                    Debug.Log("저는 1004번입니다");
                    break;

                case 1005:

                    Debug.Log("저는 1005번입니다");
                    break;

                case 1006:

                    Debug.Log("저는 1006번입니다");
                    break;

                case 1007:

                    Debug.Log("저는 1007번입니다");
                    break;

                case 1008:

                    Debug.Log("저는 1008번입니다");
                    break;

                case 1009:

                    Debug.Log("저는 1009번입니다");
                    break;

                case 1010:

                    Debug.Log("저는 1010번입니다");
                    break;

                case 1011:

                    Debug.Log("저는 1011번입니다");
                    break;

                case 1012:

                    Debug.Log("저는 1012번입니다");
                    break;

                case 1013:

                    Debug.Log("저는 1013번입니다");
                    break;

                case 1014:

                    Debug.Log("저는 1014번입니다");
                    break;

                case 1015:

                    Debug.Log("저는 1015번입니다");
                    break;

            }

            return;
        }
    }
}