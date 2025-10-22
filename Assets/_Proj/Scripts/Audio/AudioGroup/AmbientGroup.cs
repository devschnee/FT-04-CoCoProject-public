using UnityEngine;
using UnityEngine.Audio;

public class AmbientGroup : MonoBehaviour, IAudioGroup
{
    [Header("Pooling Settings")]
    [SerializeField] private int poolSize = 5;

    private AudioMixer mixer;
    private AudioMixerGroup group;
    private AmbientPlayer player;
    private AudioPool audioPool;

    private void Awake()
    {
        mixer = AudioManager.AudioGroupProvider.GetMixer();
        group = AudioManager.AudioGroupProvider.GetGroup(AudioType.Ambient);
        Debug.Log($"AmbientGroup : {group}");
        audioPool = new AudioPool(transform, group, poolSize);
        player = new AmbientPlayer(mixer, transform, audioPool);
    }

    // 오디오 실행
    public void PlayAmbient(AudioClip clip, bool loop, bool pooled, Vector3? pos = null)
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
