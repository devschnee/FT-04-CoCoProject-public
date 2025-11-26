using UnityEngine;

public class Dialogue : MonoBehaviour
{
    private string dialogueId;
    private bool isread;

    private PlayerMovement playerMovement;

    public void Init(string id)
    {
        dialogueId = id;
        Debug.Log($"[Dialogue] Init 완료 → ID: {dialogueId}");
    }

    void OnTriggerEnter(Collider other)
    {
        if (UserData.Local.preferences.skipDialogues == true) return;
        if (isread) return;
        if (!other.CompareTag("Player")) return;
        playerMovement = other.GetComponent<PlayerMovement>();
        Joystick joystick = FindAnyObjectByType<Joystick>();
        if (joystick != null)
        {
            // KHJ - Dialogue Panel이 켜졌으니 조이스틱 입력 잠금
            joystick.IsLocked = true;
        }
        playerMovement.enabled = false;

        DialogueManager.Instance.NewDialogueMethod(dialogueId);

        DialogueManager.Instance.playerMovement = playerMovement;
        DialogueManager.Instance.OnDialogueEnd += () =>
        {
            if (playerMovement != null)
                playerMovement.enabled = true;
            if(joystick != null)
                joystick.IsLocked = false;
        };

        isread = true;
    }
}