using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// NOTE, TODO : 최종 시점(카메라) 변경 후 UI를 시점에 맞게 rotation 설정해줘야 함. 현재는 0,0,0. 라인 76
[DisallowMultipleComponent]
public class Buffalo : MonoBehaviour, IPlayerFinder
{
    [Header("Timer & Jump")]
    [Tooltip("버튼 누른 뒤 버팔로 충격파 실행까지 대기 시간")]
    public float interactionSeconds = 0.1f;
    [Tooltip("충격 전 점프 연출에 걸리는 시간(Interaction 이후 시간초 시작")]
    public float jumpDuration = 0.35f;
    [Tooltip("점프 높이 곡선 0~1 비율")]
    public AnimationCurve jumpY = AnimationCurve.EaseInOut(0, 0, 1, 0.5f);
    
    [Header("Cooldown")]
    [Tooltip("쿨타임")]
    public float coolTime = 5f;
    public Image btnImg;
    public Sprite defaultSprite;
    public Sprite timerSprite;
    public Image coolTimeFillImg;

    [Header("Visual")]
    public RingRange ring;
    public Button interactionBtn;

    [Header("Player Detection")]
    public float detectRadius = 3f; // 플레이어 감지 범위
    public LayerMask playerLayer; // 플레이어 레이어 마스크
    private Transform playerTrans; // 감지된 플레이어의 Transform

    [SerializeField] private Shockwave shockwave;

    bool running;
    bool onCooldown;

    Transform IPlayerFinder.Player { get => playerTrans; set => playerTrans = value; }

    // LSH 추가 1126
    public event Action OnBombStart;

    void Awake()
    {
        interactionBtn.gameObject.SetActive(false);
        ring.gameObject.SetActive(false);

        if (!shockwave) shockwave = GetComponent<Shockwave>();

        btnImg = interactionBtn.targetGraphic as Image;

        btnImg.enabled = true;

        if (coolTimeFillImg)
        {
            coolTimeFillImg.enabled = false;
            coolTimeFillImg.fillAmount = 0f;
        }

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            playerTrans = playerGO.transform;
        }
    }

    void Start()
    {
        if (ring == null) ring = GetComponentInChildren<RingRange>(true);
        btnImg.sprite = defaultSprite;
        SetBtnInteractable(true);
    }

    void Update()
    {
        DetectPlayer();
    }

    void LateUpdate()
    {
        // NOTE, TODO : 최종 시점(카메라) 변경 후 UI를 시점에 맞게 rotation 설정해줘야 함. 현재는 0,0,0
        if (interactionBtn)
        {
            // World Rotation을 Quaternion.identity(X=0, Y=0, Z=0)로 설정
            interactionBtn.transform.rotation = Quaternion.Euler(75f, 0, 0);
        }
    }

    // 플레이어 감지
    void DetectPlayer()
    {
        if (!playerTrans || !interactionBtn || !ring) return;

        // 대화가 생성되면 버튼을 숨김
        if (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive)
        {
            if (interactionBtn.gameObject.activeSelf)
                interactionBtn.gameObject.SetActive(false);
            return;
        }

        float distance = Vector3.Distance(transform.position + Vector3.up * 0.5f, playerTrans.position);
        bool inRange = distance <= detectRadius;

        if (interactionBtn.gameObject.activeSelf != inRange)
        {
            interactionBtn.gameObject.SetActive(inRange);
            ring.gameObject.SetActive(inRange);
        }
    }

    public void Interact()
    {
        if (running || onCooldown) return;
        // LSH 추가 1127 ETCEvent.Invoke... => 소리
        ETCEvent.InvokeCocoInteractSoundInGame();
        StartCoroutine(WaveRunCoroutine());
        StartCoroutine(CooldownCoroutine());
    }

    void OnMouseDown() => Interact();

    IEnumerator WaveRunCoroutine()
    {
        // LSH 추가 1126
        OnBombStart?.Invoke();
        running = true;

        float tile = Mathf.Max(0.01f, shockwave? shockwave.tileHeight : 1f);

        // 타이머
        float t = 0f;
        while (t < interactionSeconds) { t += Time.deltaTime; yield return null; }

        // 점프
        yield return StartCoroutine(JumpCoroutine(tile));

        // 충격파
        shockwave.Fire();
        Debug.Log($"[Buffalo] Shockwave.Fire at {transform.position}", this);
        running = false;
    }

    IEnumerator JumpCoroutine(float tile)
    {
        var p0 = transform.position; // 시작 위치
        float t = 0f;
        while (t < jumpDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / jumpDuration); // 0~1 정규화 진행도
            float addY = jumpY.Evaluate(u) * tile; // 실제 상승량
            transform.position = new Vector3(p0.x, p0.y + addY, p0.z);
            yield return null;
        }
        transform.position = p0; // 끝나면 원위치
    }


    void SetBtnInteractable(bool on)
    {
        if (interactionBtn) interactionBtn.interactable = on;
    }

    IEnumerator CooldownCoroutine()
    {
        onCooldown = true;

        btnImg.sprite = timerSprite;

        if (coolTimeFillImg)
        {
            coolTimeFillImg.enabled = true;
            coolTimeFillImg.fillAmount = 1f; // 100%에서 시작
        }

        SetBtnInteractable(false);

        float cd = Mathf.Max(0.01f, coolTime);
        float t = 0f;
        while (t < cd)
        {
            t += Time.deltaTime;
            if (coolTimeFillImg)
            {
                // 1 -> 0 으로 감소(남은 시간 비율)
                coolTimeFillImg.fillAmount = Mathf.Clamp01(1f - (t / cd));
            }
            yield return null;
        }

        btnImg.sprite = defaultSprite;

        // 쿨 종료 : 기본 이미지(스킬)로 복귀 + 오버레이 숨김 + 버튼 활성화
        if (coolTimeFillImg)
        {
            coolTimeFillImg.fillAmount = 0f;
            coolTimeFillImg.enabled = false;
        }
        SetBtnInteractable(true);

        onCooldown = false;
    }
}
