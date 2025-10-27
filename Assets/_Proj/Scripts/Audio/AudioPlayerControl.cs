using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI는 깍두기 제어 X
/// 
/// BGM, Cutscene, Voice는 오디오소스 1개만 있으니
/// 이것만 제어하면 됨(클립교체하는 구조니 삭제 X)
/// 
/// SFX, Ambient는 Pool이 있는데 태그로 새로 생성 풀 식별 가능. 그래서 새로 생성 풀은 초기화시 삭제하는 구조
/// 
/// 초기화 시 모든 오디오소스의 클립을 뺌. 소스의 볼륨과 피치를 다시 초기화,
/// 기획 쪽에서 일반 대화 시 캐릭터 소리 빼고는 모든 소리를 줄이길 원함, 절반정도. 근데 일단 안할끄
/// </summary>
public abstract class AudioPlayerControl
{
    public List<AudioSource> activeSources = new List<AudioSource>();

    public virtual void PlayAll()
    {
        foreach (var src in activeSources)
        {
            if (src != null && !src.isPlaying) src.Play();
        }
    }

    public virtual void PauseAll()
    {
        foreach (var src in activeSources)
        {
            if (src != null && src.isPlaying) src.Pause();
        }
    }

    public virtual void ResumeAll()
    {
        foreach (var src in activeSources)
        {
            if (src != null && !src.isPlaying) src.UnPause();
        }
    }
    public virtual void StopAll()
    {
        foreach (var src in activeSources)
        {
            if (src != null && src.isPlaying) 
            {
                src.DOFade(0, 1f);
                src.Stop();
            } 
        }
    }

    public virtual void ResetAll()
    {
        foreach (var src in activeSources)
        {
            if (src != null)
            {
                if (src.isPlaying) src.Stop();
                src.loop = false;
                src.volume = 1f;
                src.pitch = 1f;
                src.clip = null;
            }
        }
    }

}
