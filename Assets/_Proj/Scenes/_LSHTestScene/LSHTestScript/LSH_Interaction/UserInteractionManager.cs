using System;
using UnityEngine;

public class UserInteractionManager : MonoBehaviour
{
    // 이건 어떻게 쓸까
    public static UserInteractionManager Instance { get; private set; }

    public event Action<ILobbyInteractable> OnInteracted;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void InteractionEvent(ILobbyInteractable target)
    {
        OnInteracted?.Invoke(target);
    }
}
