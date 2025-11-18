using UnityEngine;
using UnityEngine.Audio;

public class DialogueGroup : MonoBehaviour, IAudioController
{
    private AudioMixer mixer;
    private AudioMixerGroup bgm;
    private AudioMixerGroup sfx;
    private DialoguePlayer player;
    private AudioSource audioS;

    public void Init()
    {
        mixer = AudioManager.AudioGroupProvider.GetMixer();
        bgm = AudioManager.AudioGroupProvider.GetGroup(AudioType.DialogueBGM);
        sfx = AudioManager.AudioGroupProvider.GetGroup(AudioType.DialogueSFX);
        Debug.Log($"BGMGroup : {bgm}, SFXGroup : {sfx}");
        player = new DialoguePlayer(mixer, transform, bgm, sfx);
    }
    // 일단 임시로 파라미터를 AudioType type, string audioFileName으로 함
    // DialogeData.cs에는 enum으로 SoundType이 있음, 아마 다이얼로그 소리는
    // AudioType을 SoundType으로 변경해야할 듯
    // 이유는 다이얼로그 오디오 부분은 Resources에서 BGM이면 DialogueBGM 폴더 SFX이면 DialogueSFX 폴더 이니
    // 근데 StageData.cs에 GetAudio가 있고 StageProvider.cs에 GetAudioClip이 있음.
    // 이건 해당 스테이지 시퀀스에 박혀있는 오디오 스트링 값을 가져오는 것 같은데 어떻게 해야하지?
    // Dialogue.cs, DialogueData.cs, DialogueParser.cs, DialogueProvider.cs, StageData.cs, StageProvide.cs
    // StageDatabase로 만든 
    // 흠.. 생각을 해보자
    public void PlayDialogue(AudioType type, string audioFileName)
    {
        player.PlayDialogueAudio(type, audioFileName);
    }

    public void PostInit()
    {
        throw new System.NotImplementedException();
    }
    public void PlayPlayer()
    {
        throw new System.NotImplementedException();
    }
    public void PausePlayer()
    {
        throw new System.NotImplementedException();
    }
    public void ResumePlayer()
    {
        throw new System.NotImplementedException();
    }
    public void StopPlayer()
    {
        throw new System.NotImplementedException();
    }
    public void ResetPlayer()
    {
        throw new System.NotImplementedException();
    }
}
