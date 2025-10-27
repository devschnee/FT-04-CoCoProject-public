using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AudioEvents
{
    // Enum(BGMKey, SFXKey 등등), int(해당 키의 클립 인덱스, -1이면 랜덤재생), float(fadeIn : BGM, Cutscene 전용), float(fadeOut : BGM, Cutscene 전용), bool(loop 소리 반복 유무), bool(오브젝트풀링 : SFX, Ambient 전용), Vector3?(위치 : SFX, Ambient 전용 값이 있으면 해당 위치에서 재생(3d) 없으면 그냥 2d재생)
    public static event Action<Enum, int, float, float, bool, bool, Vector3?> OnPlayAudio;

    public static void Raise(Enum key, int index = -1, float fadeIn = 0, float fadeOut = 0, bool loop = false, bool pooled = false, Vector3? pos = null)
    {
        OnPlayAudio?.Invoke(key, index, fadeIn, fadeOut, loop, pooled, pos);
    }
}
