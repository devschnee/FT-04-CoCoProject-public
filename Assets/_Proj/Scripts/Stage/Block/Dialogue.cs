using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

public class Dialogue : MonoBehaviour
{
    private string dialogueId;
    private bool isRead = false;

    private DialogueData currentData;
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool isDialogueActive = false;

    private int currentSeq = 0; // dialogue 내 순번

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

        currentSeq = 1;
        ShowDialogue(dialogueId, currentSeq);
        isDialogueActive = true;
    }
    void Update()
    {
        if (!isDialogueActive) return;
        //if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
        //{
        //    OnUserTap();
        //}
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
        currentData = DataManager.Instance.Dialogue.GetData(dialogueId);
        if (currentData == null)
        {
            Debug.Log($"[Dialogue] {id} seq {seq} 데이터 없음 → 종료 처리");
            EndDialogue();
            return;
        }

        var speakData = DataManager.Instance.Speaker.GetData(currentData.speaker_id);
        var speakerSprite = DataManager.Instance.Speaker.GetPortrait(currentData.speaker_id, speakData.portrait_set_prefix);

        // 화자 이미지 갱신
        if (currentData.speaker_position == SpeakerPosition.left)
        {
            StageUIManager.Instance.DialogueSpeakerLeft.sprite = speakerSprite;
            StageUIManager.Instance.DialogueSpeakerRight.color = new Color(1, 1, 1, 0.2f);
        }
        else
        {
            StageUIManager.Instance.DialogueSpeakerRight.sprite = speakerSprite;
            StageUIManager.Instance.DialogueSpeakerLeft.color = new Color(1, 1, 1, 0.2f);
        }

        // 표정 변경
        UpdateEmotion(currentData.speaker_id, currentData.emotion);

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
        var nextData = DataManager.Instance.Dialogue.GetData($"{dialogueId}_{nextSeq}");
        if (nextData == null)
        {
            // 마지막 대사
            EndDialogue();
        }
        else
        {
            currentSeq = nextSeq;
            ShowDialogue(dialogueId, currentSeq);
        }
    }

    // 감정 표현 업데이트
    private void UpdateEmotion(SpeakerData.SpeakerId id, EmotionType emotion)
    {
        // TODO: emotion값에 따라 sprite나 animator 변경
        Debug.Log($"[Emotion] {id} → {emotion}");
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
        Debug.Log("[Dialogue] 대화 종료");
    }
}
