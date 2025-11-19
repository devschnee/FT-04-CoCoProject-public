using UnityEngine;
using UnityEngine.Audio;

public class SFXGroup : MonoBehaviour, IAudioController
{
    [Header("Pooling Settings")]
    [SerializeField] private int poolSize = 10;

    private AudioMixer mixer;
    private AudioMixerGroup group;
    private SFXPlayer player;
    private AudioPool audioPool;

    public void PlaySFX(AudioClip clip, bool loop, bool pooled, Vector3? pos = null)
    {
        player.PlayAudio(clip, group, loop, pooled, pos);
    }

    // IAudioController 영역
    public void Init()
    {
        mixer = AudioManager.AudioGroupProvider.GetMixer();
        group = AudioManager.AudioGroupProvider.GetGroup(AudioType.SFX);
        Debug.Log($"SFXGroup : {group}");
        audioPool = new AudioPool(transform, group, poolSize);
        player = new SFXPlayer(mixer, transform, audioPool);
    }
    public void PostInit() { }
    public void PlayPlayer()
    {
        player.PlayAll();
    }

    public void PausePlayer()
    {
        player.PauseAll();
    }

    public void ResumePlayer()
    {
        player.ResumeAll();
    }

    public void StopPlayer()
    {
        player.StopAll();
    }

    public void ResetPlayer()
    {
        player.ResetAll();
    }

    public void SetVolumeHalf()
    {
        
    }

    public void SetVolumeNormal()
    {
        
    }
}
