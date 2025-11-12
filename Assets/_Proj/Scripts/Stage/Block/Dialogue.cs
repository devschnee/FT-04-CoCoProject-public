using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Dialogue : MonoBehaviour
{
    private string dialogueId;
    private bool isRead = false;

    private DialogueData currentData;
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool isDialogueActive = false;

    private int currentSeq = 0; // dialogue 내 순번

    private PlayerMovement playerMovement;

    public void Init(string id)
    {
        dialogueId = id;
        Debug.Log($"[Dialogue] Init 완료 → ID: {dialogueId}");
    }
    void Start()
    {
        if(StageUIManager.Instance.stageManager.isTest)
        {
            dialogueId = "dialogue_1_1_1";
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isRead) return;
        if (!other.CompareTag("Player")) return;

        isRead = true;
        StageUIManager.Instance.Overlay.SetActive(true);
        StageUIManager.Instance.DialoguePanel.SetActive(true);
        StageUIManager.Instance.OptionOpenButton.gameObject.SetActive(false);

        currentSeq = 0;
        ShowDialogue(dialogueId, currentSeq);
        isDialogueActive = true;

        playerMovement = other.GetComponent<PlayerMovement>();
        playerMovement.enabled = false;
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
            TryNextDialogue();
        }
    }

    // 대사 출력
    private void ShowDialogue(string id, int seq)
    {
        currentData = DataManager.Instance.Dialogue.GetSeqData(seq);
        if (currentData == null)
        {
            Debug.Log($"[Dialogue] {id} seq {seq} 데이터 없음 → 종료 처리");
            EndDialogue();
            return;
        }

        var speakData = DataManager.Instance.Speaker.GetData(currentData.speaker_id);
        var basePrefix = speakData.portrait_set_prefix;

        var emotionSprite = GetEmotionSprite(currentData.speaker_id, basePrefix);

        // 화자 이미지 갱신
        if (currentData.speaker_position == SpeakerPosition.left)
        {
            StageUIManager.Instance.DialogueSpeakerLeft.color = new Color(1, 1, 1, 1);
            StageUIManager.Instance.DialogueSpeakerLeft.sprite = emotionSprite;
            if (currentData.seq == 0)
            {
                StageUIManager.Instance.DialogueSpeakerRight.gameObject.SetActive(false);
            }
            else
            {
                StageUIManager.Instance.DialogueSpeakerRight.gameObject.SetActive(true);
                StageUIManager.Instance.DialogueSpeakerRight.color = new Color(1, 1, 1, 0.2f);
            }
        }
        else
        {
            StageUIManager.Instance.DialogueSpeakerRight.gameObject.SetActive(true);
            StageUIManager.Instance.DialogueSpeakerRight.color = new Color(1, 1, 1, 1);
            StageUIManager.Instance.DialogueSpeakerRight.sprite = emotionSprite;
            StageUIManager.Instance.DialogueSpeakerLeft.color = new Color(1, 1, 1, 0.2f);
        }

        // 이름, 텍스트 초기화
        StageUIManager.Instance.DialogueNameText.text = speakData.display_name;
        StageUIManager.Instance.DialogueText.text = "";

        // 타자기 효과
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(StageUIManager.Instance.DialogueText, currentData.text, currentData.char_delay));
    }

    //다음 대사 시도
    private void TryNextDialogue()
    {
        int nextSeq = currentData.seq + 1;
        var nextData = DataManager.Instance.Dialogue.GetSeqData(nextSeq);

        if (nextData == null)//Todo : 조건 변경해야 할듯 이대로는 이상함
        {
            // 마지막 대사
            EndDialogue();
        }
        else if(nextData.seq == currentData.seq + 1)
        {
            currentSeq = nextSeq;
            ShowDialogue(dialogueId, currentSeq);
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

        Debug.Log($"[Emotion] {id} → ({emotionKey})");
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
        StageUIManager.Instance.DialoguePanel.SetActive(false);
        StageUIManager.Instance.Overlay.SetActive(false);
        StageUIManager.Instance.OptionOpenButton.gameObject.SetActive(true);
        playerMovement.enabled = true;
        Debug.Log("[Dialogue] 대화 종료");
    }
}
