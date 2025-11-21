using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// AudioEvents.cs���� ���� ��� AudioManager���� ���� ��Ź.
// key(���� ������), pooled(������Ʈ Ǯ������), position(2d �Ҹ����� 3d �Ҹ�����)
public class AudioEventListener : MonoBehaviour
{
    public static AudioEventListener Instance { get; private set; }
    // DDOL�ϳ� ������ �ϴ� �̱������� �������, �̱������� ������ ���ϰ� �̺�Ʈ ���Ÿ�
    // �ٵ� AudioManager�� ���δٸ� ���� �̱��� �ʿ���?

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("�ߺ� ���� ���ŵ�");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("AudioEventListener ����");
    }

    private void OnEnable()
    {
        AudioEvents.OnPlayAudio += HandlePlayAudio;
        AudioEvents.OnPlayDialogue += HandlePlayDialogueSound;
        AudioEvents.OnPlayStageBGM += HandleStageBGM;

    }

    private void OnDisable()
    {
        AudioEvents.OnPlayAudio -= HandlePlayAudio;
        AudioEvents.OnPlayDialogue -= HandlePlayDialogueSound;
        AudioEvents.OnPlayStageBGM -= HandleStageBGM;

    }


    private void HandleStageBGM(AudioClip clip)
    {
        AudioManager.Instance.PlayStageBGM(clip);
    }

    private void HandlePlayAudio(Enum key, int index = -1, float fadeIn = 0, float fadeOut = 0, bool loop = false, bool pooled = false, Vector3? pos = null)
    {
        AudioManager.Instance.PlayAudio(key, index, fadeIn, fadeOut, loop, pooled, pos);
    }

    // public void PlayAudio(Enum key, int index = -1, float fadeIn = 0, float fadeOut = 0, bool loop = false, bool pooled = false, Vector3? pos = null)
    // {
    //     AudioManager.Instance.PlayAudio(key, index, fadeIn, fadeOut, loop, pooled, pos);
    // }

    // 일단 임시로 만듦
    private void HandlePlayDialogueSound(AudioType type, string audioFileName)
    {
        AudioManager.Instance.PlayDialogueAudio(type, audioFileName);
        //DataManager.Instance.Stage.GetAudioClip(,);
    }
}

