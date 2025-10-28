using UnityEngine;

public class InLobbyManager : MonoBehaviour
{
    public TestScriptableObject[] objectDatabase;

    public static InLobbyManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        foreach (var data in objectDatabase)
        {
            GameObject obj = Instantiate(data.prefab);
            var meta = obj.GetComponent<TestObjectMeta>();
            meta.Initialize(data);
        }
    }
}
