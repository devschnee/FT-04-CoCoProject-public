using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// UI�� ��α� ���� X
/// 
/// BGM, Cutscene, Voice�� ������ҽ� 1���� ������
/// �̰͸� �����ϸ� ��(Ŭ����ü�ϴ� ������ ���� X)
/// 
/// SFX, Ambient�� Pool�� �ִµ� �±׷� ���� ���� Ǯ �ĺ� ����. �׷��� ���� ���� Ǯ�� �ʱ�ȭ�� �����ϴ� ����
/// 
/// �ʱ�ȭ �� ��� ������ҽ��� Ŭ���� ��. �ҽ��� ������ ��ġ�� �ٽ� �ʱ�ȭ,
/// ��ȹ �ʿ��� �Ϲ� ��ȭ �� ĳ���� �Ҹ� ������ ��� �Ҹ��� ���̱� ����, ��������. �ٵ� �ϴ� ���Ҳ�
/// </summary>
public abstract class AudioPlayerControl
{
    public List<AudioSource> activeSources = new List<AudioSource>();

    public virtual void PlayAll()
    {
        foreach (var src in activeSources)
        {
            if (src != null && !src.isPlaying)
            {
                if (DOTween.IsTweening(src, true))
                {
                    src.DOKill();
                }
                if (src.volume != 1)
                {
                    src.Play();
                    src.DOFade(1, 0.5f);
                }
                else
                {
                    src.Play();
                }
            }
        }
    }

    public virtual void PauseAll()
    {
        foreach (var src in activeSources)
        {
            if (src != null && src.isPlaying)
            {
                if (DOTween.IsTweening(src, true))
                {
                    src.DOKill();
                }
                src.DOFade(0, 0.5f).OnComplete(() => src.Pause());
            }
        }
    }

    public virtual void ResumeAll()
    {
        foreach (var src in activeSources)
        {
            if (src != null && !src.isPlaying)
            {
                if (DOTween.IsTweening(src, true))
                {
                    src.DOKill();
                }
                src.UnPause();
                src.DOFade(1, 0.5f);
            }
        }
    }
    public virtual void StopAll()
    {
        foreach (var src in activeSources)
        {
            if (src != null && src.isPlaying) 
            {
                if (DOTween.IsTweening(src, true))
                {
                    src.DOKill();
                }
                src.DOFade(0, 0.5f).OnComplete(() => src.Stop());
            } 
        }
    }

    public virtual void ResetAll()
    {
        foreach (var src in activeSources)
        {
            if (src != null)
            {
                if (src.isPlaying) 
                {
                    if (DOTween.IsTweening(src, true))
                    {
                        src.DOKill();
                    }
                    src.DOFade(0, 0.3f).OnComplete(() => {src.Stop(); src.volume = 1f;});
                }
                src.loop = false;
                src.volume = 1f;
                src.pitch = 1f;
                src.clip = null; 
            }
        }
    }

    public virtual void SetVolumeZero()
    {
        foreach (var src in activeSources)
        {
            if (DOTween.IsTweening(src, true))
            {
                src.DOKill();
            }
            src.DOFade(0, 0.5f);
        }
    }

    public virtual void SetVolumeHalf()
    {
        foreach (var src in activeSources)
        {
            if (DOTween.IsTweening(src, true))
            {
                src.DOKill();
            }
            src.DOFade(0.3f, 0.5f);
        }
    }

    public virtual void SetVolumeNormal()
    {
        foreach (var src in activeSources)
        {
            if (DOTween.IsTweening(src, true))
            {
                src.DOKill();
            }
            src.DOFade(1, 0.5f);
        }
    }

}
