using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

[Serializable]
public class DecoData
{
    public int id;
    public string name;
    public string prefabPath;
    public string iconPath;
    public Category category;
    public string tag; //todo : 향후 enum으로 수정 예정
    public Acquire acquire;
    public int stack;
    public string description;

    [NonSerialized] public GameObject prefab;
    [NonSerialized] public Sprite icon;

    //DataTable에 있는 변수들을 가져와서 DecoData 클래스에 선언
    //다른 DataTable도 각각의 클래스를 만들어서 DataTable에 있는 변수들을 갖게 제작

    public GameObject GetPrefab()
    {
        if (prefab == null && !string.IsNullOrEmpty(prefabPath))
            prefab = Resources.Load<GameObject>(prefabPath);
        return prefab;
    }

    public Sprite GetIcon()
    {
        if (icon == null && !string.IsNullOrEmpty(iconPath))
            icon = Resources.Load<Sprite>(iconPath);
        return icon;
    }
    //Deco의 prefab과 sprite가 필요할 때 사용할 함수
}
public enum Category
{
    plant, strucure, furniture, fixtures
}
public enum Acquire
{
    quest, ingame, shop
}