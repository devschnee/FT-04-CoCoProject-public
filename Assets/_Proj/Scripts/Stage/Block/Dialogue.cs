using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

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
        if (isread) return;
        if (!other.CompareTag("Player")) return;

        playerMovement = other.GetComponent<PlayerMovement>();
        playerMovement.enabled = false;

        DialogueManager.Instance.NewDialogueMethod(dialogueId);

        DialogueManager.Instance.playerMovement = playerMovement;
        DialogueManager.Instance.OnDialogueEnd += () =>
        {
            if (playerMovement != null)
                playerMovement.enabled = true;
        };

        isread = true;
    }
}