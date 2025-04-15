// 윙슈트 효과 클래스
using UnityEngine;

public class WingSuitEffect : CostumeEffectBase
{
    public float hoverForce = 5f;
    public float hoverDuration = 3f;
    public float energyCostPerSecond = 10f;

    private bool isHovering;
    private float hoverTimeRemaining;
    private Rigidbody2D playerRb;

    private void Start()
    {
        hoverTimeRemaining = hoverDuration;
        playerRb = FindObjectOfType<PlayerMovement>()?.GetComponent<Rigidbody2D>();
    }

    public override void ActivateEffect()
    {
        this.enabled = true;
    }

    public override void DeactivateEffect()
    {
        isHovering = false;
        this.enabled = false;
    }

    private void Update()
    {
        if (!this.enabled || playerRb == null) return;

        // 입력 확인 (예: Space 키)
        if (Input.GetKeyDown(KeyCode.Space) && hoverTimeRemaining > 0)
        {
            isHovering = true;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            isHovering = false;
        }

        // 부양 중이면 에너지 소모 및 시간 감소
        if (isHovering)
        {
            float energyCost = energyCostPerSecond * Time.deltaTime;

            // TODO: 에너지 시스템 연결
            // if (EnergySystem.UseEnergy(energyCost))
            {
                hoverTimeRemaining -= Time.deltaTime;
                if (hoverTimeRemaining <= 0)
                {
                    isHovering = false;
                }
            }
            // else
            // {
            //     // 에너지 부족
            //     isHovering = false;
            // }
        }
        else
        {
            // 부양 중이 아니면 시간 회복
            hoverTimeRemaining = Mathf.Min(hoverTimeRemaining + Time.deltaTime * 0.5f, hoverDuration);
        }
    }

    private void FixedUpdate()
    {
        if (isHovering && playerRb != null)
        {
            // 중력에 반하는 힘 적용
            playerRb.AddForce(Vector2.up * hoverForce, ForceMode2D.Force);
        }
    }
}