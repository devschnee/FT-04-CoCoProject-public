using UnityEngine;
using UnityEngine.Audio;

public class UIGroup : MonoBehaviour, IAudioGroup
{
    private AudioMixer mixer;
    private AudioMixerGroup group;
    private UIPlayer player;

    private void Awake()
    {
        mixer = AudioManager.AudioGroupProvider.GetMixer();
        group = AudioManager.AudioGroupProvider.GetGroup(AudioType.UI);
        Debug.Log($"UIGroup.cs : {group}, SFX그룹이면 OK");
        player = new UIPlayer(mixer, transform);
    }

    // 오디오 실행
    public void PlayVoice(AudioClip clip)
    {
        player.PlayAudio(clip, group);
    }

    // UI 쪽은 어떤 상태든 제어에서 자유로운 몸이니 굳이 필요없을 듯?
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
