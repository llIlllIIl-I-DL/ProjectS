using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System; // Exception 클래스 사용을 위해 추가
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI Instance { get; private set; }

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

    [Header("GameOver Window")]
    public float fadeSpeed = 1.5f;

    [SerializeField] public GameObject gameOverWindow;
    [SerializeField] public Transform gameOverWindowParents;
    [SerializeField] public CanvasGroup fadeOut;

    GameObject _gameOverWindow;
    CanvasGroup _fadeOut;


    [Header("HP 바")]
    [SerializeField] private Scrollbar healthBar;
    [SerializeField] private Image healthBarImage;
    [SerializeField] private Image healLight;

    [SerializeField] private TextMeshProUGUI hpText;

    [Header("HP 바 깨짐")]
    [SerializeField] private GameObject Basic;
    [SerializeField] private GameObject Hurt;
    [SerializeField] private GameObject VeryHurt;
    [SerializeField] private GameObject killme;

    private float shakeTime;
    private float shakeRange;

    [Header("Ammo 바 업데이트")]
    [SerializeField] public Scrollbar ammoBar;
    [SerializeField] public Image ammoBarImage;
    [SerializeField] public Image ammoBarLight;
    


    [Header("플레이어 속성 아이콘 업데이트")]
    [SerializeField] public ItemData attributeType;
    [SerializeField] public TextMeshProUGUI typeName;
    [SerializeField] public Image typeIcon;

    [Header("Utility Point")]
    [SerializeField] public TextMeshProUGUI utilityPointText;


    private Player player;
    private BoxCollider2D playercollider;

    private PlayerHP playerHP;
    private TypeItemSlotList typeItemSlotList;

    public Dictionary<ItemData, Sprite> TypeItemDic = new Dictionary<ItemData, Sprite>();

    public void Start()
    {
        player = FindObjectOfType<Player>();
        playerHP = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerHP>();
        playercollider = GameObject.FindGameObjectWithTag("Player").GetComponent<BoxCollider2D>();

        // HP 변경 이벤트 구독
        playerHP.OnHPChanged += OnPlayerHPChanged;

        float maxHP = playerHP.MaxHP;
        float currentHP = playerHP.CurrentHP;

        int ammo = WeaponManager.Instance.AmmoManager.CurrentAmmo;
        int maxAmmo = WeaponManager.Instance.AmmoManager.MaxAmmo;

        

        // 탄약 변경 이벤트 구독
        if (WeaponManager.Instance != null)
        {
            WeaponManager.Instance.OnAmmoChanged += UpdateAmmoUI;
        }
        // 초기 Ammo UI 업데이트
        UpdateAmmoUI(ammo, maxAmmo);

        Vector3 realPosition = healthBar.transform.position;
        Debug.Log($"{realPosition}");

        healthBarImage.fillAmount = 1f;

        // 인벤토리 매니저 이벤트 구독 - 무기 속성 변경 시 UI 업데이트
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnWeaponAttributeChanged += UpdateWeaponAttributeUI;
        }

        UpdatePlayerHPInUItext();
        SetHealthBar(playerHP.MaxHP, playerHP.CurrentHP);
        // 초기 무기 속성 설정
        UpdateWeaponAttributeUI(InventoryManager.Instance.EquippedWeaponAttribute);
    }

    private void OnPlayerHPChanged(float maxHP, float currentHP)
    {
        SetHealthBar(maxHP, currentHP);
        UpdatePlayerHPInUItext();
    }

    public void Voscuro(Vector3 realPosition, float maxHP, float currentHP)
    {
        HealHP();
    }

    public void UpdatePlayerHPInUItext()
    {
        hpText.text = playerHP.CurrentHP.ToString();
    }

    public void SetHealthBar(float maxHP, float currentHP) //여기 언젠가 리팩토리 필요...
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
            float x = UnityEngine.Random.value * shakeRange - (shakeRange / 2);
            float y = UnityEngine.Random.value * shakeRange - (shakeRange / 2);

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

    // 무기 속성 UI 업데이트 메소드
    private void UpdateWeaponAttributeUI(ItemData weaponAttribute)
    {
        if (weaponAttribute != null)
        {
            attributeType = weaponAttribute;
            typeName.text = weaponAttribute.ItemName;
            typeIcon.sprite = weaponAttribute.Icon;

            typeIcon.preserveAspect = true;
        }
        else
        {
            // 기본 노말 속성으로 되돌리기
            // 기본 아이템 데이터 참조 필요
        }
    }


    public void MovetoLeftType()
    {
        // 인벤토리에서 사용 가능한 무기 속성 목록 가져오기
        List<ItemData> availableTypes = null;
        if (InventoryManager.Instance != null)
        {
            availableTypes = InventoryManager.Instance.GetWeaponAttributes();
        }
        
        if (availableTypes == null || availableTypes.Count <= 1) return;
        
        // 현재 장착된 무기 속성 찾기
        ItemData currentType = attributeType;
        int currentIndex = -1;
        
        for (int i = 0; i < availableTypes.Count; i++)
        {
            if (availableTypes[i].elementType == currentType.elementType)
            {
                currentIndex = i;
                break;
            }
        }
        
        // 이전 인덱스 계산 (순환)
        int prevIndex = (currentIndex - 1 + availableTypes.Count) % availableTypes.Count;
        
        // 이전 무기 속성 장착
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.EquipWeaponAttribute(availableTypes[prevIndex]);
            Debug.Log($"무기 속성 변경: {availableTypes[prevIndex].ItemName}");
        }
    }

    public void MovetoRightType()
    {
        // 인벤토리에서 사용 가능한 무기 속성 목록 가져오기
        List<ItemData> availableTypes = null;
        if (InventoryManager.Instance != null)
        {
            availableTypes = InventoryManager.Instance.GetWeaponAttributes();
        }
        
        if (availableTypes == null || availableTypes.Count <= 1) return;
        
        // 현재 장착된 무기 속성 찾기
        ItemData currentType = attributeType;
        int currentIndex = -1;
        
        for (int i = 0; i < availableTypes.Count; i++)
        {
            if (availableTypes[i].elementType == currentType.elementType)
            {
                currentIndex = i;
                break;
            }
        }
        
        // 다음 인덱스 계산 (순환)
        int nextIndex = (currentIndex + 1) % availableTypes.Count;
        
        // 다음 무기 속성 장착
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.EquipWeaponAttribute(availableTypes[nextIndex]);
            Debug.Log($"무기 속성 변경: {availableTypes[nextIndex].ItemName}");
        }
    }

    public void AddUtilityPoint(int utilityPointForOneWay)
    {
        player.utilityPoint += utilityPointForOneWay;
        utilityPointText.text = player.utilityPoint.ToString();
    }

    public void TempAddUtilityPoint()
    {
        utilityPointText.text = player.utilityPoint.ToString();
    }

    // 탄약 UI를 업데이트하는 메서드
    private void UpdateAmmoUI(int ammo, int maxAmmo)
    {
        float ratio = (float)ammo / maxAmmo;
        ammoBar.value = ratio;
        ammoBarImage.fillAmount = ratio;
    }



    public void ShowGameOverUI()
    {
        try
        {
            // 모든 캔버스 찾기
            Canvas worldCanvas = null;
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            
            foreach (Canvas canvas in allCanvases)
            {
                // 메인 캔버스 또는 월드 캔버스 찾기
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || 
                    canvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    worldCanvas = canvas;
                    break;
                }
            }
            
            if (worldCanvas == null)
            {
                Debug.LogWarning("캔버스를 찾을 수 없습니다. UI가 제대로 표시되지 않을 수 있습니다.");
            }
            
            // 부모 없이 먼저 인스턴스화
            _fadeOut = Instantiate(fadeOut);
            if (_fadeOut != null)
            {
                _fadeOut.alpha = 0;
                
                // 찾은 캔버스를 부모로 설정
                if (worldCanvas != null)
                {
                    _fadeOut.transform.SetParent(gameOverWindowParents);
                }
                
                StartCoroutine(FadeOut(_fadeOut));
            }
            else
            {
                Debug.LogError("fadeOut 인스턴스 생성 실패");
                // 실패해도 시작 씬으로 이동
                StartCoroutine(DelayedSceneLoad(4.0f));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"게임오버 UI 생성 중 오류 발생: {e.Message}");
            // 오류가 발생해도 씬 전환 시도
            StartCoroutine(DelayedSceneLoad(4.0f));
        }
    }

    IEnumerator FadeOut(CanvasGroup _fadeOutCanvas)
    {
        if (_fadeOutCanvas == null)
        {
            Debug.LogError("FadeOut: _fadeOutCanvas is null");
            yield break;
        }

        float alpha = _fadeOutCanvas.alpha;

        yield return new WaitForSeconds(1);

        while (alpha < 1f)
        {
            alpha += Time.deltaTime * fadeSpeed;
            _fadeOutCanvas.alpha = alpha;
            yield return null;
        }

        if (gameOverWindow != null)
        {
            // 대신 월드 캔버스를 찾아서 사용
            Canvas worldCanvas = null;
            
            // 먼저 모든 캔버스를 찾습니다
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in allCanvases)
            {
                // 메인 캔버스 또는 월드 캔버스 찾기
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || 
                    canvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    worldCanvas = canvas;
                    break;
                }
            }
            
            if (worldCanvas == null)
            {
                Debug.LogWarning("캔버스를 찾을 수 없습니다. UI가 제대로 표시되지 않을 수 있습니다.");
            }

            // 부모 없이 인스턴스화
            GameObject gameOverWindowInstance = null;
            
            try
            {
                // 항상 부모 없이 인스턴스화
                gameOverWindowInstance = Instantiate(gameOverWindow);
                
                // 안전하게 월드 캔버스를 사용하여 부모 설정
                if (worldCanvas != null && gameOverWindowInstance != null)
                {
                    gameOverWindowInstance.transform.SetParent(gameOverWindowParents);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"게임오버 윈도우 생성 오류: {e.Message}");
            }

            _gameOverWindow = gameOverWindowInstance;
            yield return new WaitForSeconds(3);
            
            // 씬 전환
            SceneManager.LoadScene("StartScene", LoadSceneMode.Single);
            Debug.Log("스타트씬 이동!!");
        }
    }


    // 지연된 씬 로드 코루틴
    private IEnumerator DelayedSceneLoad(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene("StartScene", LoadSceneMode.Single);
    }

    public void ToStartMenu(GameObject _gameOverWindow) // 이 메소드는 더 이상 필요 없습니다.

    {
        SceneManager.LoadScene("StartScene", LoadSceneMode.Single);
        Debug.Log("스타트씬 이동!!");
    }
}