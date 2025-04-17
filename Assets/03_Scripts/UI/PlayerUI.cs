using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUI : Singleton<PlayerUI>
{
    [Header("HP 바")]
    [SerializeField] private Scrollbar healthBar;
    [SerializeField] private Image healthBarImage;
    [SerializeField] private Image healLight;

    [SerializeField] private int healHP;

    [Header("HP 바 깨짐")]
    [SerializeField] public GameObject Basic;
    [SerializeField] public GameObject Hurt;
    [SerializeField] public GameObject VeryHurt;
    [SerializeField] public GameObject killme;

    public float shakeTime;
    public float shakeRange;

    [Header("플레이어 속성 아이콘 업데이트")]
    [SerializeField] public ItemData attributeType;
    [SerializeField] public TextMeshProUGUI typeName;
    [SerializeField] public Image typeIcon;

    static PlayerHP playerHP;
    public TypeItemSlotList typeItemSlotList;

    static int currentTypeIndex = 0;

    public Dictionary<ItemData, Sprite> TypeItemDic = new Dictionary<ItemData, Sprite>();

    public void Start()
    {
        playerHP = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerHP>();

        float maxHP = playerHP.MaxHP;
        float currentHP = playerHP.CurrentHP;

        Vector3 realPosition = healthBar.transform.position;
        Debug.Log($"{realPosition}");

        healthBarImage.fillAmount = 1f;
    }

    public void Voscuro(Vector3 realPosition, float maxHP, float currentHP)
    {
        HealHP();
    }

    public void SetHealthBar(float maxHP, float currentHP)
    {
        StartCoroutine(ShakingHPBar());

        float currentHPAmount = (float)currentHP / maxHP;

        healthBarImage.fillAmount = currentHPAmount;

        float hpBalance = (float)currentHP / maxHP;

        if (hpBalance > 0.9f)
        {
            Basic.SetActive(true);
            Hurt.SetActive(false);
            VeryHurt.SetActive(false);
            killme.SetActive(false);
        }

        else if (hpBalance > 0.7f && hpBalance < 0.9f)
        {
            Basic.SetActive(false);
            Hurt.SetActive(true);
            VeryHurt.SetActive(false);
            killme.SetActive(false);
        }

        else if (hpBalance > 0.3f && hpBalance < 0.7f)
        {
            Basic.SetActive(false);
            Hurt.SetActive(false);
            VeryHurt.SetActive(true);
            killme.SetActive(false);
        }

        else if (hpBalance < 0.3f)
        {
            Basic.SetActive(false);
            Hurt.SetActive(false);
            VeryHurt.SetActive(false);
            killme.SetActive(true);
        }

    }

    public IEnumerator ShakingHPBar()
    {

        float elapsed = 0.0f;
        Vector3 originalPosition = healthBar.transform.position;


        while (elapsed < shakeTime)
        {
            elapsed += Time.deltaTime;
            float x = Random.value * shakeRange - (shakeRange / 2);
            float y = Random.value * shakeRange - (shakeRange / 2);

            healthBar.transform.position = new Vector3(originalPosition.x + x, originalPosition.y + y, originalPosition.z);
            yield return null;
        }

        healthBar.transform.position = originalPosition;
    }

    public void HealHP()
    {
        float maxHP = playerHP.MaxHP;
        float currentHP = playerHP.CurrentHP;

        Vector3 realPosition = healthBar.transform.position;


        healthBar.transform.position = new Vector3(realPosition.x, realPosition.y, realPosition.z);


        if (currentHP > maxHP)
        {
            return;
        }

        StartCoroutine(HealHPBarLighting());
        SetHealthBar(maxHP, currentHP);
    }

    public IEnumerator HealHPBarLighting()
    {
        byte alpha = 0;

        for (int i = 0; i < 5; i++)
        {
            alpha += 51;
            yield return new WaitForSeconds(0.05f);
            healLight.color = new Color32(255, 255, 255, alpha);
        }

        healLight.color = new Color32(255, 255, 255, 0);
    }

    public void MovetoLeftType()
    {
            Debug.Log("왼발 왼발왼발왼발~");
            Debug.Log(TypeItemDic.Count);
    }

    public void MovetoRightType()
    {
        Debug.Log("오른발 오른발 오른발 오른발");
    }
}