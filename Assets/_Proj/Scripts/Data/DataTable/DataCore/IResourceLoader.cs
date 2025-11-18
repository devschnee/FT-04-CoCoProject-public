using UnityEngine;

public interface IResourceLoader
{
    //Resource 로딩 인터페이스로 추상화, Resource에서 Load 할 항목이 생긴다면 이곳에 먼저 추가
    GameObject LoadPrefab(string path);
    Sprite LoadSprite(string path);
    Material LoadMaterial(string path);
    AudioClip LoadAudio(string path);
}