using System.Collections;
using UnityEngine;

public class SimpleOverlay : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return null;
        gameObject.SetActive(false);
    }
}
