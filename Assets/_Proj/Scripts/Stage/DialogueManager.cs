using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public System.Action OnDialogueEnd;
    public bool isDialogueActive = false;
    public PlayerMovement playerMovement;

    private string dialogueId;
    private bool isRead = false;

    private DialogueData currentData;
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool hasLeftSpeaker = false;
    private bool hasRightSpeaker = false;

    private int currentSeq = 0; // dialogue 내 순번
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool NewDialogueMethod(string id)
    {
        if (UserData.Local.preferences.skipDialogues == true) return false;
        if (isRead) return false;

        isRead = true;
        StageUIManager.Instance.Overlay.SetActive(true);
        StageUIManager.Instance.DialoguePanel.SetActive(true);
        StageUIManager.Instance.OptionOpenButton.gameObject.SetActive(false);

        dialogueId = id;

        currentSeq = 0;
        AnalyzeDialogueSideUsage(dialogueId);
        //LSH 추가
        AudioManager.Instance.EnterDialogue();
        ShowDialogue(dialogueId, currentSeq);
        isDialogueActive = true;

        return true;
    }

    void Update()
    {
        if (!isDialogueActive) return;

        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.wasPressedThisFrame) // 터치 “시작” 순간만 true
                {
                    OnUserTap();
                    break;
                }
            }
        }
    }

    // 유저 터치 처리
    private void OnUserTap()
    {
        var dialogueText = StageUIManager.Instance.DialogueText;

        if (isTyping)
        {
            // 아직 출력 중이라면 즉시 전부 출력
            StopCoroutine(typingCoroutine);
            dialogueText.text = currentData.text;
            isTyping = false;
        }
        else
        {
            // 이미 다 출력됐다면 다음 대사로
            TryNextDialogue(dialogueId);
        }
    }

    // 대사 출력
    private void ShowDialogue(string id, int seq)
    {
        // 현재 데이터 불러오기
        currentData = DataManager.Instance.Dialogue.GetSeqData(id, seq);
        if (currentData == null)
        {
            Debug.Log($"[Dialogue] {id} seq {seq} 데이터 없음 → 종료 처리");
            EndDialogue();
            return;
        }

        // 화자 정보
        var speakData = DataManager.Instance.Speaker.GetData(currentData.speaker_id);

        // 스프라이트 prefix
        var basePrefix = $"Talk_portrait/{currentData.speaker_id}_{currentData.emotion}_{currentData.speaker_position}";
        var emotionSprite = GetEmotionSprite(currentData.speaker_id, basePrefix);

        // 여기서 자동 UI 처리
        UpdateSpeakerUI(currentData, emotionSprite);

        // 이름 표시
        StageUIManager.Instance.DialogueNameText.text = speakData.display_name;

        // 텍스트 초기화
        StageUIManager.Instance.DialogueText.text = "";

        // 이전 타이핑 중단
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        // 타이핑 시작
        typingCoroutine = StartCoroutine(
            TypeText(StageUIManager.Instance.DialogueText, currentData.text, currentData.char_delay)
        );
        //LSH 추가, 소리 재생을 여기서 합니꽈?
        PlaySoundFromData(currentData);
    }

    private void UpdateSpeakerUI(DialogueData currentData, Sprite emotionSprite)
    {
        bool isLeft = currentData.speaker_position == SpeakerPosition.Left;

        if (currentData.seq == 0)
        {
            if (isLeft)
                StageUIManager.Instance.DialogueSpeakerRight.gameObject.SetActive(false);
            else
                StageUIManager.Instance.DialogueSpeakerLeft.gameObject.SetActive(false);
        }

        // 다음 seq 화자
        var nextData = DataManager.Instance.Dialogue.GetSeqData(dialogueId, currentData.seq + 1);
        bool nextSameSide = nextData != null &&
                            nextData.speaker_position == currentData.speaker_position;

        // 왼쪽 화자 아예 없는 경우 처리
        if (!hasLeftSpeaker)
            StageUIManager.Instance.DialogueSpeakerLeft.gameObject.SetActive(false);

        // 오른쪽 화자 아예 없는 경우 처리
        if (!hasRightSpeaker)
            StageUIManager.Instance.DialogueSpeakerRight.gameObject.SetActive(false);

        // ===== 실제 UI 처리 =====
        if (isLeft)
        {
            // 왼쪽 화자 표시
            StageUIManager.Instance.DialogueSpeakerLeft.gameObject.SetActive(true);
            StageUIManager.Instance.DialogueSpeakerLeft.color = new Color(1, 1, 1, 1);
            StageUIManager.Instance.DialogueSpeakerLeft.sprite = emotionSprite;
            if (currentData.seq == 0) return;

            // 오른쪽 처리
            if (!hasRightSpeaker)
            {
                // 오른쪽이 존재하지 않는 데이터라면 숨김
                StageUIManager.Instance.DialogueSpeakerRight.gameObject.SetActive(false);
            }
            else if (nextSameSide)
            {
                // 다음도 왼쪽 → 오른쪽 숨김
                StageUIManager.Instance.DialogueSpeakerRight.gameObject.SetActive(false);
            }
            else
            {
                // 다음 화자가 오른쪽 → 흐리게 표시
                StageUIManager.Instance.DialogueSpeakerRight.gameObject.SetActive(true);
                StageUIManager.Instance.DialogueSpeakerRight.color = new Color(1, 1, 1, 0.2f);
            }
        }
        else
        {
            // 오른쪽 화자 표시
            StageUIManager.Instance.DialogueSpeakerRight.gameObject.SetActive(true);
            StageUIManager.Instance.DialogueSpeakerRight.color = new Color(1, 1, 1, 1);
            StageUIManager.Instance.DialogueSpeakerRight.sprite = emotionSprite;
            if (currentData.seq == 0) return;

            // 왼쪽 처리
            if (!hasLeftSpeaker)
            {
                StageUIManager.Instance.DialogueSpeakerLeft.gameObject.SetActive(false);
            }
            else if (nextSameSide)
            {
                StageUIManager.Instance.DialogueSpeakerLeft.gameObject.SetActive(false);
            }
            else
            {
                StageUIManager.Instance.DialogueSpeakerLeft.gameObject.SetActive(true);
                StageUIManager.Instance.DialogueSpeakerLeft.color = new Color(1, 1, 1, 0.2f);
            }
        }
    }

    //다음 대사 시도
    private void TryNextDialogue(string id)
    {
        if (currentData == null)
        {
            EndDialogue();
            return;
        }

        int nextSeq = currentData.seq + 1;
        var nextData = DataManager.Instance.Dialogue.GetSeqData(id, nextSeq);

        if (nextData == null)
        {
            // 마지막 대사
            EndDialogue();
        }
        else if (nextData.seq == currentData.seq + 1)
        {
            ShowDialogue(id, nextSeq);
        }
    }

    // 감정 표현 업데이트
    private Sprite GetEmotionSprite(SpeakerData.SpeakerId id, string basePrefix)
    {
        // 예: prefix가 "coco_"라면 "coco_Happy", "coco_Sad" 식으로 찾기
        string emotionKey = $"{basePrefix}";
        var sprite = DataManager.Instance.Speaker.GetPortrait(id, emotionKey);
        if (sprite == null)
        {
            // 해당 감정 이미지 없으면 기본 표정
            sprite = DataManager.Instance.Speaker.GetPortrait(id, basePrefix);
        }

        return sprite;
    }

    // TextMeshPro 타이핑 효과 함수
    private IEnumerator TypeText(TextMeshProUGUI textComponent, string fullText, float delay)
    {
        isTyping = true;
        textComponent.text = "";
        foreach (char c in fullText)
        {
            textComponent.text += c;
            yield return new WaitForSeconds(delay);
        }
        isTyping = false;
    }

    // 대화 종료
    private void EndDialogue()
    {
        isDialogueActive = false;
        isRead = false;
        StageUIManager.Instance.DialoguePanel.SetActive(false);
        StageUIManager.Instance.Overlay.SetActive(false);
        StageUIManager.Instance.OptionOpenButton.gameObject.SetActive(true);
        Debug.Log("[Dialogue] 대화 종료");

        //LSH 추가
        AudioManager.Instance.ExitDialogue();

        if(playerMovement != null)
            playerMovement.enabled = true;

        OnDialogueEnd?.Invoke();
    }

    private void AnalyzeDialogueSideUsage(string id)
    {
        int seq = 0;
        DialogueData data;

        while ((data = DataManager.Instance.Dialogue.GetSeqData(id, seq)) != null)
        {
            if (data.speaker_position == SpeakerPosition.Left)
                hasLeftSpeaker = true;
            else
                hasRightSpeaker = true;

            seq++;
        }
    }
    //LSH 추가 소리부분
    private void PlaySoundFromData(DialogueData data)
    {
        if (string.IsNullOrEmpty(data.sound_key)) return;
        AudioType audioType;
        switch (data.sound_type)
        {
            case SoundType.bgm :
            audioType = AudioType.DialogueBGM;
            break;
            case SoundType.sfx :
            audioType = AudioType.DialogueSFX;
            break;
            case SoundType.sfx_none :
            default :
            return;
        }
        AudioEvents.RaiseDialogueSound(audioType, data.sound_key);
    }
}
