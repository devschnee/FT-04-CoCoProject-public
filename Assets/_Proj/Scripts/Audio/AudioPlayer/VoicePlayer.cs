// using UnityEngine;
// using UnityEngine.Audio;
// // 더미테이더
// public class VoicePlayer : AudioPlayerControl
// {
//     private readonly AudioMixer mixer;
//     private readonly Transform myTrans;
//     private AudioSource currentSource;

//     public VoicePlayer(AudioMixer mixer, Transform myTrans)
//     {
//         this.mixer = mixer;
//         this.myTrans = myTrans;
//     }

//     public void PlayAudio(AudioClip clip, AudioMixerGroup group)
//     {
//         if (currentSource == null)
//         {
//             GameObject gObj = new GameObject($"VoicePlayer");
//             gObj.transform.parent = myTrans;
//             currentSource = gObj.AddComponent<AudioSource>();
//             activeSources.Add(currentSource);
//             currentSource.outputAudioMixerGroup = group;
//             //Object.DontDestroyOnLoad(gObj);
//         }

//         //if (currentSource.isPlaying && currentSource.clip == clip) return;

//         currentSource.clip = clip;
//         currentSource.Play();
//     }
// }
