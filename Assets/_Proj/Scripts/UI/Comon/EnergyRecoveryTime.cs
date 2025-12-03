using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class EnergyRecoveryTime : MonoBehaviour
{
    public TextMeshProUGUI energyRecoveryTime;
    private bool isRunning = false;

    void OnEnable()
    {
        StartRecoveryTimer();
    }

    void OnDisable()
    {
        StopAllCoroutines();
        isRunning = false;
    }

    void StartRecoveryTimer()
    {
        if (isRunning) return;
        StartCoroutine(CoUpdateEnergyTimer());
        isRunning = true;
    }

    IEnumerator CoUpdateEnergyTimer()
    {
        while (true)
        {
            int currentEnergy = UserData.Local.goods[GoodsType.energy];

            if (currentEnergy >= 5)
            {
                isRunning = false;
                energyRecoveryTime.text = $"최대!";
                yield break;
            }

            long lastEnergyTime = UserData.Local.master.lastEnergyTime;
            DateTimeOffset lastOffset = DateTimeOffset.FromUnixTimeSeconds(lastEnergyTime);

            TimeSpan elapsed = DateTimeOffset.UtcNow - lastOffset;
            int recoverSeconds = 1800; //

            // 다음 1개 회복까지 남은 시간
            int remainSec = recoverSeconds - (int)elapsed.TotalSeconds;

            int min = remainSec / 60;
            int sec = remainSec % 60;

            energyRecoveryTime.text = $"{min:00}:{sec:00}";

            yield return new WaitForSeconds(1f);
        }
    }
}