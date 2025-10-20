using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AudioEvents
{
    // key(음악 뭐할지), position(2d 소리할지 3d 소리할지), pooled(오브젝트 풀링인지)
    public static event Action<SFXKey, Vector3, bool> OnPlaySFX;

    public static void Raise(SFXKey key, Vector3 pos, bool pooled = true)
    {
        OnPlaySFX?.Invoke(key, pos, pooled);
    }
}
