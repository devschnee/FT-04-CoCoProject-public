using UnityEngine;

public enum PlaceableCategory { Home, Animal, Deco, MainCharacter }

public interface IPlaceableData
{
    int Id { get; }
    string DisplayName { get; }
    Sprite GetIcon(ResourcesLoader loader);
    GameObject GetPrefab(ResourcesLoader loader);
}

// ─────────────────────────────────────────────────────
// 각 DB 데이터 클래스를 감싸는 어댑터들
// ─────────────────────────────────────────────────────
public sealed class HomePlaceable : IPlaceableData
{
    private readonly HomeData _d;                 // 실제 클래스명은 프로젝트 것 사용
    public HomePlaceable(HomeData d) => _d = d;
    public int Id => _d.home_id;
    public string DisplayName => _d.home_name;
    public Sprite GetIcon(ResourcesLoader l) => _d.GetIcon(l);
    public GameObject GetPrefab(ResourcesLoader l) => _d.GetPrefab(l);
}

public sealed class AnimalPlaceable : IPlaceableData
{
    private readonly AnimalData _d;
    public AnimalPlaceable(AnimalData d) => _d = d;
    public int Id => _d.animal_id;
    public string DisplayName => _d.animal_name;
    public Sprite GetIcon(ResourcesLoader l) => _d.GetIcon(l);
    public GameObject GetPrefab(ResourcesLoader l) => _d.GetPrefab(l);
}

public sealed class DecoPlaceable : IPlaceableData
{
    private readonly DecoData _d;
    public DecoPlaceable(DecoData d) => _d = d;
    public int Id => _d.deco_id;
    public string DisplayName => string.IsNullOrEmpty(_d.deco_name) ? $"Deco {_d.deco_id}" : _d.deco_name;
    public Sprite GetIcon(ResourcesLoader l) => _d.GetIcon(l);
    public GameObject GetPrefab(ResourcesLoader l) => _d.GetPrefab(l);
}
