using UnityEngine;

public class ResourcesLoader : IResourceLoader
{
    //인터페이스를 구현하는 클래스, 이곳에서 Resource.Load를 담당
    public GameObject LoadPrefab(string path) => Resources.Load<GameObject>(path);
    public Sprite LoadSprite(string path) => Resources.Load<Sprite>(path);
    public Material LoadMaterial(string path) => Resources.Load<Material>(path);
    public AudioClip LoadAudio(string path) => Resources.Load<AudioClip>(path);
}