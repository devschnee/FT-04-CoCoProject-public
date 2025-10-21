using System;
using System.Collections.Generic;
using UnityEngine;


public class AudioLibraryProvider : IAudioLibraryProvider 
{
    private readonly Dictionary<AudioType, IAudioLibrary> libraryMap;

    public AudioLibraryProvider(
        BGMLibrary bgm,
        SFXLibrary sfx,
        AmbientLibrary ambient,
        CutsceneLibrary cutscene,
        VoiceLibrary voice)
    {
        libraryMap = new Dictionary<AudioType, IAudioLibrary>()
        {
            { AudioType.BGM, bgm },
            { AudioType.SFX, sfx },
            { AudioType.Ambient, ambient },
            { AudioType.Cutscene, cutscene },
            { AudioType.Voice, voice }
        };
    }

    public AudioClip GetClip(AudioType type, Enum key, int index = -1)
    {
        if (libraryMap.TryGetValue(type, out var lib))
        {
            return lib.GetClipByEnum(key, index);
        }

        Debug.LogWarning($"{type} ¾ø¾î");
        return null;
    }
}
