using BossFSM;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BossUI : MonoBehaviour
{
    [SerializeField] private Collider2D bossRoomCollider;
    [SerializeField] private GameObject bossHealthUI;

    [SerializeField] private Slider bossHealthBar;
    [SerializeField] private BossHealth bossHealth;

    [SerializeField] private Canvas bossClear;

    private GameObject _bossHealthUI;
    public GameObject BossHealthUI => _bossHealthUI;

    public void Awake()
    {
        _bossHealthUI = Instantiate(bossHealthUI);

        bossHealthBar = _bossHealthUI.gameObject.GetComponentInChildren<Slider>();

        _bossHealthUI.SetActive(false);
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        {
            if (collision.CompareTag("Player"))
            {
                Debug.Log("보스룸 진입!!");

                Destroy(bossRoomCollider);

                ShowBossHealthUI();
            }
        }
    }

    public void ShowBossHealthUI()
    {
        _bossHealthUI.SetActive(true);

        UpdateBossHealthUI();
    }

    public void UpdateBossHealthUI()
    {
        float currentHPAmount = (float)bossHealth.CurrentHP / bossHealth.maxHP;

        bossHealthBar.value = currentHPAmount;

        if (bossHealth.CurrentHP == 0)
        {
            BossClear();
        }
    }

    public void BossClear()
    {
        Image slider = _bossHealthUI.gameObject.GetComponentInChildren<Image>();

        bossHealthBar.gameObject.SetActive(false);

        slider.color = Color.white;

        StartCoroutine(BossClearStart());
    }
    
    public IEnumerator BossClearStart()
    {
        
        Time.timeScale = 0.3f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        
        yield return new WaitForSeconds(1f);

        
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        

        Destroy(_bossHealthUI);
        Debug.Log("보스 클리어!");

        yield return new WaitForSeconds(3f);

        BossClearPopUp();
    } 

    public void BossClearPopUp()
    {
        bossClear.gameObject.SetActive(true);
    }

    public void CloseGame()
    {
        Application.Quit();
        Debug.Log("나갔다!");
    }
}