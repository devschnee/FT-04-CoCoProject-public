using System;
using UnityEngine;

/// <summary>
/// Audio Key = 5가지
/// Audio Type = 5가지 
/// 키를 입력 받으면 오디오 타입이 정해지는게 좋을 것 같지.
/// </summary>

public interface IAudioLibrary
{
    AudioClip GetClipByEnum(Enum key, int index = -1);
}

public interface IAudioLibraryProvider
{
    AudioClip GetClip(AudioType type, Enum key, int index = -1);
}

public interface IAudioState
{
    void Enter(AudioManager audio);
    void Exit(AudioManager audio);
    void Update(AudioManager audio);
}