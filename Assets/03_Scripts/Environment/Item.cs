using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    public ItemData Itemdata => itemData;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("아이템 움직임")]
    [SerializeField] private float floatAmplitude = 0.1f;
    [SerializeField] private float floatFrequency = 1f;
    private Vector3 startPosition;

    [Header("수집 설정")]
    [SerializeField] private float collectCooldown = 0.5f; // 아이템 생성 후 수집 가능까지의 쿨다운
    private bool isCollectable = true;

    [Header("특성 슬롯")]
    [SerializeField] public InvenSlotUI invenSlotUI;

    Player player;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        player = FindObjectOfType<Player>();

        // 아이템 생성 직후 잠시 수집 불가능 상태로 설정
        StartCoroutine(EnableCollection());
    }

    private void Start()
    {
        if (itemData != null)
        {
            UpdateVisual();
        }

        startPosition = transform.position;
    }

    private void Update()
    {
        // 아이템 위아래 움직임
        if (gameObject.activeSelf)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    // 아이템 데이터 설정
    public void SetItemData(ItemData data)
    {
        this.itemData = data;
        UpdateVisual();
    }

    // 아이템 데이터 가져오기
    public ItemData GetItemData()
    {
        return itemData;
    }

    // 시각적 요소 업데이트
    private void UpdateVisual()
    {
        if (spriteRenderer != null && itemData != null && itemData.Icon != null)
        {
            spriteRenderer.sprite = itemData.Icon;
        }
    }

    // 수집 가능 상태로 전환
    private IEnumerator EnableCollection()
    {
        isCollectable = false;
        yield return new WaitForSeconds(collectCooldown);
        isCollectable = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isCollectable) return;

        if (other.CompareTag("Player"))
        {
            CollectItem(other.gameObject);
        }
    }

    // 아이템 수집 처리
    private void CollectItem(GameObject player)
    {
        if (itemData == null) return;

        bool collected = false;

        // 아이템 타입에 따라 다르게 처리
        switch (itemData.itemType)
        {
            case ItemType.UsableItem:
                // 즉시 사용 아이템은 바로 효과 적용
                if (itemData.itemUsageType == ItemUsageType.InstantUse)
                {
                    collected = ApplyInstantEffect(player);
                }
                // 인벤토리에 저장되는 아이템
                else
                {
                    collected = StoreInInventory();
                }
                break;

            case ItemType.CostumeParts:
            case ItemType.WeaponAttribute:
                // 파츠, 무기 속성은 인벤토리에 저장
                collected = StoreInInventory();
                break;

            default:
                collected = StoreInInventory();
                break;
        }

        if (collected)
        {
            // 아이템 오브젝트 비활성화 및 제거
            gameObject.SetActive(false);
            Destroy(gameObject, 0.1f);
        }
    }

    // 즉시 사용 아이템 효과 적용
    private bool ApplyInstantEffect(GameObject player)
    {
        // 회복 아이템인 경우
        if (itemData.itemAttributeType == ItemAttributeType.HealItem)
        {
            PlayerHP playerHP = player.GetComponent<PlayerHP>();
            if (playerHP != null)
            {
                playerHP.Heal(itemData.effectValue);
                Debug.Log($"{itemData.ItemName}을(를) 획득하여 {itemData.effectValue}만큼 체력을 즉시 회복했습니다.");
                return true;
            }
        }
        // 최대 체력 증가 아이템인 경우
        else if (itemData.itemAttributeType == ItemAttributeType.MaxHPUpItem)
        {
            PlayerHP playerHP = player.GetComponent<PlayerHP>();
            if (playerHP != null)
            {
                playerHP.IncreaseMaxHP(itemData.effectValue);
                Debug.Log($"{itemData.ItemName}을(를) 획득하여 최대 체력이 {itemData.effectValue}만큼 증가했습니다.");
                return true;
            }
        }
        // 그 외 아이템은 ItemManager에 위임
        else if (ItemManager.Instance != null)
        {
            return ItemManager.Instance.UseItem(itemData);
        }

        return false;
    }

    // 인벤토리에 아이템 저장
    private bool StoreInInventory()
    {
        if (ItemManager.Instance != null)
        {
            bool added = ItemManager.Instance.AddItem(itemData);

            // 파츠 아이템인 경우 UI에 알림 표시 (예시)
            if (added && itemData.itemType == ItemType.CostumeParts)
            {
                Debug.Log($"<color=cyan>새로운 복장 파츠 획득:</color> {itemData.ItemName}");
                // 필요시 UI 팝업이나 알림 기능 호출
                // NotificationManager.ShowNotification($"복장 파츠 획득: {itemData.ItemName}");
            }

            if (added && itemData.itemType == ItemType.UtilityPoint)
            {
                PlayerUI.Instance.AddUtilityPoint(itemData.utilityPointForNow);
                CreatSlotSystem.Instance.RefreshAllOwnPoints();
            }

                return added;
        }

        return false;
    }
}