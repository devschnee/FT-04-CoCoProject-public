using UnityEngine;
using UnityEngine.Audio;

public class AmbientGroup : MonoBehaviour, IAudioController
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
        Debug.Log($"AmbientGroup Mixer : {mixer.name}");
        group = AudioManager.AudioGroupProvider.GetGroup(AudioType.Ambient);
        Debug.Log($"AmbientGroup MixerGroup : {group}");
        audioPool = new AudioPool(transform, group, poolSize);
        player = new AmbientPlayer(mixer, transform, audioPool);
    }

    // 오디오 실행
    public void PlayAmbient(AudioClip clip, bool loop, bool pooled, Vector3? pos = null)
    {
        player.PlayAudio(clip, group, loop, pooled, pos);
    }

    // 오디오 제어
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
}
