using UnityEngine;
using UnityEngine.Audio;

public class DialoguePlayer : AudioPlayerControl
{
    private readonly AudioMixer mixer;
    private readonly Transform myTrans;
    public AudioSource diaBGM;
    public AudioSource diaSFX;

    private const string BGMFolderPath = "Sound/DialogueBGM/";
    private const string SFXFolderPath = "Sound/DialogueSFX/";

    public DialoguePlayer(AudioMixer mixer, Transform myTrans, AudioMixerGroup bgm, AudioMixerGroup sfx)
    {
        this.mixer = mixer;
        this.myTrans = myTrans;

        GameObject gObj = new GameObject("DialogueBGM");
        gObj.transform.parent = myTrans;
        diaBGM = gObj.AddComponent<AudioSource>();
        activeSources.Add(diaBGM);
        diaBGM.outputAudioMixerGroup = bgm;
        diaBGM.volume = 1;

        GameObject gObj2 = new GameObject("DialogueSFX");
        gObj2.transform.parent = myTrans;
        diaSFX = gObj2.AddComponent<AudioSource>();
        activeSources.Add(diaSFX);
        diaSFX.outputAudioMixerGroup = sfx;
        diaSFX.volume = 1;
    }

    public void PlayDialogueAudio(AudioType type, string audioFileName)
    {
        if (!string.IsNullOrEmpty(audioFileName))
        {
            switch (type)
            {
                case AudioType.DialogueBGM :
                AudioClip bgmClip = Resources.Load<AudioClip>(BGMFolderPath + audioFileName);
                    if (diaBGM.clip == bgmClip && diaBGM.isPlaying) return;
                    if (bgmClip != null)
                    {
                        diaBGM.clip = bgmClip;
                        diaBGM.loop = true;
                        diaBGM.Play();
                    }
                break;
                case AudioType.DialogueSFX :
                AudioClip sfxClip = Resources.Load<AudioClip>(SFXFolderPath + audioFileName);
                    if (sfxClip != null)
                    {
                        //diaSFX.clip = sfxClip; // OneShot 형식이 아니라면
                        diaSFX.PlayOneShot(sfxClip);
                    }
                break;
            }
        }
        else { }
    }
    
    public override void PlayAll()
    {
        base.PlayAll();
    }
    public override void PauseAll()
    {
        base.PauseAll();
    }
    public override void ResumeAll()
    {
        base.ResumeAll();
    }
    public override void StopAll()
    {
        base.StopAll();
    }
    public override void ResetAll()
    {
        base.ResetAll();
    }
    public override void SetVolumeHalf()
    {
        // ?필요없음
    }
    public override void SetVolumeNormal()
    {
        // ?필요없음
    }
}
