using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LMasterRouteManager
{
    private Transform startPoint;
    private List<Transform> decoList = new List<Transform>();
    private int currentIndex = 0;
    public bool hasComplete = false;


    public LMasterRouteManager(Transform startPoint)
    {
        this.startPoint = startPoint;
    }

    public void RefreshDecoList()
    {
        List<Transform> oneShotList = new();
        GameObject[] decos = GameObject.FindGameObjectsWithTag("Decoration");
        foreach (GameObject d in decos)
        {
            oneShotList.Add(d.transform);
        }
        oneShotList.Sort((a, b) => Vector3.Distance(startPoint.transform.position, a.position).CompareTo(Vector3.Distance(startPoint.transform.position, b.position)));
        decoList = oneShotList;

        currentIndex = 0;
        hasComplete = false;
    }

    public Transform GetNextDeco()
    {
        if (currentIndex >= decoList.Count)
        {
            hasComplete = true;
        }
        if (decoList == null || decoList.Count == 0) return null;
        if (hasComplete)
        {
            currentIndex = 0;
            return null;
        }
        Transform next = decoList[currentIndex];
        currentIndex++;
        return next;
    }
}
