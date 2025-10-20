using UnityEngine;

/// <summary>
/// Audio Key = 5가지
/// Audio Type = 5가지 
/// 키를 입력 받으면 오디오 타입이 정해지는게 좋을 것 같지.
/// </summary>

public interface IBGMPlayer
{
    void PlayBGM(AudioClip clip, float fadeInTime = 0f, float fadeOutTime = 0f, bool loop = true);
}

// 말이 SFX이지
internal interface ISFXPlayer
{
    void PlayOneShotSFX();
}

public interface IAudioVolumeChange
{

}

public interface IAudioService : IBGMPlayer { }

public interface IAudioChannel
{
    void Play(AudioType type, int index = -1, Vector3? position = null);
}