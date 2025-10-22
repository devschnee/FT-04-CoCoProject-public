using UnityEngine;
using UnityEngine.Audio;

public class SFXGroup : MonoBehaviour, IAudioGroup
{
    [Header("Pooling Settings")]
    [SerializeField] private int poolSize = 10;

    private AudioMixer mixer;
    private AudioMixerGroup group;
    private SFXPlayer player;
    private AudioPool audioPool;

    private void Awake()
    {
        mixer = AudioManager.AudioGroupProvider.GetMixer();
        group = AudioManager.AudioGroupProvider.GetGroup(AudioType.SFX);
        Debug.Log($"SFXGroup : {group}");
        audioPool = new AudioPool(transform, group, poolSize);
        player = new SFXPlayer(mixer, transform, audioPool);
    }

    // 오디오 실행
    public void PlaySFX(AudioClip clip, bool loop, bool pooled, Vector3? pos = null)
    {
        player.PlayAudio(clip, group, loop, pooled, pos);
    }

    public void Play()
    {
        throw new System.NotImplementedException();
    }

    public void Pause()
    {
        throw new System.NotImplementedException();
    }

    public void Resume()
    {
        throw new System.NotImplementedException();
    }

    public void StopAll()
    {
        throw new System.NotImplementedException();
    }

    public void ResetGroup()
    {
        throw new System.NotImplementedException();
    }
}
