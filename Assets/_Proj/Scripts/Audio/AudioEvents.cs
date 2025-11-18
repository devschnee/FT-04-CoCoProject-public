using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AudioEvents
{
    // Enum(BGMKey, SFXKey ���), int(�ش� Ű�� Ŭ�� �ε���, -1�̸� �������), float(fadeIn : BGM, Cutscene ����), float(fadeOut : BGM, Cutscene ����), bool(loop �Ҹ� �ݺ� ����), bool(������ƮǮ�� : SFX, Ambient ����), Vector3?(��ġ : SFX, Ambient ���� ���� ������ �ش� ��ġ���� ���(3d) ������ �׳� 2d���)
    public static event Action<Enum, int, float, float, bool, bool, Vector3?> OnPlayAudio;
    public static event Action<AudioType, string> OnPlayDialogue;

    public static void Raise(Enum key, int index = -1, float fadeIn = 0, float fadeOut = 0, bool loop = false, bool pooled = false, Vector3? pos = null)
    {
        OnPlayAudio?.Invoke(key, index, fadeIn, fadeOut, loop, pooled, pos);
    }
    
    // 일단 임시로 만듦
    public static void RaiseDialogueSound(AudioType type, string audioFileName)
    {
        OnPlayDialogue?.Invoke(type, audioFileName);
    }
}
