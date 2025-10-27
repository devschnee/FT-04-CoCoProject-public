using System.Collections.Generic;
using UnityEngine;
// 제어 일단 플레이어 쪽으로 만듬
//public abstract class AudioGroupBase : MonoBehaviour, IAudioController
//{

//    private HashSet<AudioSource> playerSources = new HashSet<AudioSource>();


//    public virtual void Play()
//    {
//        foreach (var src in playerSources)
//        {
//            if (src != null && !src.isPlaying) src.Play();
//        }
//    }

//    public virtual void Pause()
//    {
//        foreach (var src in playerSources)
//        {
//            if (src != null && src.isPlaying) src.Pause();
//        }
//    }

//    public virtual void Resume()
//    {
//        foreach (var src in playerSources)
//        {
//            if (src != null && !src.isPlaying) src.UnPause();
//        }
//    }
//    public virtual void Stop()
//    {
//        foreach (var src in playerSources)
//        {
//            if (src != null && src.isPlaying) src.Stop();
//        }
//    }

//    public virtual void ResetGroup()
//    {
//        foreach (var src in playerSources)
//        {
//            if (src != null)
//            {
//                if (src.isPlaying) src.Stop();
//                src.clip = null;
//            }
//        }
//        playerSources.Clear();
//    }
//}
