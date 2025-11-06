using System;
using UnityEngine;

public class Treasure : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            StageUIManager.Instance.TreasurePanel.SetActive(true);
            StageUIManager.Instance.OptionOpenButton.gameObject.SetActive(false);
            //움직임막기
        }
    }

    public void OnQuitAction(Action action)
    {
        //획득확인
    }
}
