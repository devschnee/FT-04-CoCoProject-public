using System;
using UnityEngine;

public class UserInteractionManager : MonoBehaviour
{
    
    public static UserInteractionManager Instance { get; private set; }

    public event Action<IInteractable> OnInteracted;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void InteractionEvent(IInteractable target)
    {
        OnInteracted?.Invoke(target);
    }
}
