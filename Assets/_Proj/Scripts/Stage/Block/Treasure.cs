using System;
using UnityEngine;

public class Treasure : MonoBehaviour
{
    private string treasureId;
    private bool isCollected = false;

    public void Init(string id)
    {
        treasureId = id;
        Debug.Log($"[Treasure] Init 완료 → ID: {treasureId}");

    }
    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        if (other.CompareTag("Player"))
        {
            isCollected = true;
            StageUIManager.Instance.TreasurePanel.SetActive(true);
            StageUIManager.Instance.OptionOpenButton.gameObject.SetActive(false);

            // 플레이어 이동 막기
            other.GetComponent<PlayerMovement>().enabled = false;

            // 확인 버튼 클릭 시 호출되도록 이벤트 등록
            StageUIManager.Instance.OnTreasureConfirm = () => OnQuitAction(() =>
            {
                // 획득 처리
                StageUIManager.Instance.stageManager.OnTreasureCollected(treasureId);

                // UI 닫기
                StageUIManager.Instance.TreasurePanel.SetActive(false);
                StageUIManager.Instance.OptionOpenButton.gameObject.SetActive(true);

                // 플레이어 이동 복원
                other.GetComponent<PlayerMovement>().enabled = true;
            });
        }
    }

    public void OnQuitAction(Action action)
    {
        action?.Invoke();
    }
}
