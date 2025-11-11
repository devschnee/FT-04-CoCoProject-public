using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class EditModeUISwitcherFix : MonoBehaviour
{
    [SerializeField] private EditModeUISwitcher target;

    [Header("이름 또는 경로(경로 예: UIRoot/SafeArea/LobbyMode)")]
    [SerializeField] private string lobbyNameOrPath = "LobbyMode";
    [SerializeField] private string editNameOrPath = "EditMode";

    [Header("재시도 옵션")]
    [SerializeField, Tooltip("씬 전환 후 몇 프레임 동안 시도할지")]
    private int tryFramesAfterSceneChange = 10;

    private FieldInfo fiLobby, fiEdit;

    private void Reset() { target = GetComponent<EditModeUISwitcher>(); }

    private void OnEnable()
    {
        if (!target) target = GetComponent<EditModeUISwitcher>();
        CacheFields();

        StartCoroutine(TryFixRoutine(tryFramesAfterSceneChange));
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    private void OnSceneChanged(Scene from, Scene to)
    {
        StartCoroutine(TryFixRoutine(tryFramesAfterSceneChange));
    }

    [ContextMenu("Fix Now")]
    private void FixNow() => StartCoroutine(TryFixRoutine(1));

    private IEnumerator TryFixRoutine(int frames)
    {
        for (int i = 0; i < Mathf.Max(1, frames); i++)
        {
            TryFixOnce();

            if (!IsMissing(GetArray(fiLobby)) && !IsMissing(GetArray(fiEdit)))
            {
                bool isEdit = GetIsEditMode();
                target.SendMessage(isEdit ? "HandleEnter" : "HandleExit", SendMessageOptions.DontRequireReceiver);
                yield break;
            }
            yield return null;
        }
    }

    private void TryFixOnce()
    {
        if (!target || fiLobby == null || fiEdit == null) return;

        var lobbyArr = GetArray(fiLobby);
        var editArr = GetArray(fiEdit);

        if (IsMissing(lobbyArr))
        {
            var go = FindByNameOrPath(lobbyNameOrPath);
            if (go) fiLobby.SetValue(target, new GameObject[] { go });
        }
        if (IsMissing(editArr))
        {
            var go = FindByNameOrPath(editNameOrPath);
            if (go) fiEdit.SetValue(target, new GameObject[] { go });
        }
    }

    // ---- lookups ----
    private GameObject[] GetArray(FieldInfo fi) => (GameObject[])fi.GetValue(target);

    private static bool IsMissing(GameObject[] arr)
    {
        if (arr == null || arr.Length == 0) return true;
        for (int i = 0; i < arr.Length; i++) if (arr[i] == null) return true;
        return false;
    }

    private static GameObject FindByNameOrPath(string nameOrPath)
    {
        if (string.IsNullOrEmpty(nameOrPath)) return null;

        // 경로 탐색: 루트부터 단계별 Find
        if (nameOrPath.Contains("/"))
        {
            Transform cur = null;
            // 씬 루트들부터 시작
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var r in roots)
            {
                cur = r.transform.Find(nameOrPath);
                if (cur) return cur.gameObject;
            }
            // 못 찾으면 비활성 포함 전수 검색
            var all = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (var t in all)
            {
                if (t.hideFlags != HideFlags.None) continue;
                var f = t.Find(nameOrPath);
                if (f && f.gameObject.scene.IsValid()) return f.gameObject;
            }
            return null;
        }

        // 단일 이름: 먼저 활성 오브젝트, 없으면 비활성 포함
        var go = GameObject.Find(nameOrPath);
        if (go) return go;

        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t.hideFlags != HideFlags.None) continue;           // 에셋/히든 제외
            if (!t.gameObject.scene.IsValid()) continue;           // 프리팹 에셋 제외
            if (t.name == nameOrPath) return t.gameObject;
        }
        return null;
    }

    private void CacheFields()
    {
        var t = typeof(EditModeUISwitcher);
        fiLobby = t.GetField("lobbyModeObjects", BindingFlags.NonPublic | BindingFlags.Instance);
        fiEdit = t.GetField("editModeObjects", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private static bool GetIsEditMode()
    {
        var tm = System.Type.GetType("EditModeManager");
        if (tm == null) return false;
        var pi = tm.GetProperty("IsEditMode", BindingFlags.Public | BindingFlags.Static);
        if (pi != null && pi.PropertyType == typeof(bool))
        {
            try { return (bool)pi.GetValue(null, null); } catch { }
        }
        var fi = tm.GetField("IsEditMode", BindingFlags.Public | BindingFlags.Static);
        if (fi != null && fi.FieldType == typeof(bool))
        {
            try { return (bool)fi.GetValue(null); } catch { }
        }
        return false;
    }
}
