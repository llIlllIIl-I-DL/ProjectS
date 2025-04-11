using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : Singleton<PlayerUI> 
{
    [Header("HP 바")]
    [SerializeField] private Scrollbar healthBar;
    [SerializeField] private Image healthBarImage;
    [SerializeField] private Image healLight;

    //[SerializeField] private int maxHP;
    //[SerializeField] private int currentHP;
    //[SerializeField] private int giveDamage;
    [SerializeField] private int healHP;


    [Header("HP 바 기능 테스트를 위한 임시 버튼")]
    //[SerializeField] public Button damageButton;
    [SerializeField] public Button healButton;

    [Header("HP 바 깨짐")]
    [SerializeField] public GameObject Basic;
    [SerializeField] public GameObject Hurt;
    [SerializeField] public GameObject VeryHurt;
    [SerializeField] public GameObject killme;

    private PlayerHP playerHP;

    public float shakeTime;
    public float shakeRange;

    public void Start()
    {
        playerHP = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerHP>();

        float maxHP = playerHP.MaxHP;
        float currentHP = playerHP.CurrentHP;

        Vector3 realPosition = healthBar.transform.position;
        Debug.Log($"{realPosition}");

        healthBarImage.fillAmount = 1f;

        //damageButton.onClick.AddListener(KillEmAll);
        //healButton.onClick.AddListener(() => Voscuro(realPosition, maxHP, currentHP));
    }

    public void Voscuro(Vector3 realPosition, float maxHP, float currentHP)
    {
        //SetHealthBar(maxHP, currentHP);
        HealHP();
    }

    /*
    public void KillEmAll()
    {
        currentHP -= giveDamage;


        if (currentHP < 0)
        {
            currentHP = 0;
        }

        SetHealthBar();
        StartCoroutine(ShakingHPBar());
    }
    */

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
}