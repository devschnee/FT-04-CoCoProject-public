#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[CreateAssetMenu(fileName = "DataRegistry", menuName = "GameData/DataRegistry")]
public class DataRegistry : ScriptableObject
{
    //각 데이터베이스를 한곳에 모아두는 ScriptableObject
    public AnimalDatabase animalDB;
    public ArtifactDatabase artifactDB;
    public BackgroundDatabase backgroundDB;
    public ChapterDatabase chapterDB;
    public CodexDatabase codexDB;
    public CostumeDatabase costumeDB;
    public DecoDatabase decoDB;
    public GoodsDatabase goodsDB;
    public HomeDatabase homeDB;
    public ManualDatabase manualDB;
    public Profile_iconDatabase profile_iconDB;
    public QuestDatabase questDB;
    public ShopDatabase shopDB;
    public Shop_itemDatabase shop_itemDB;
    public StageDatabase stageDB;
    public TreasureDatabase treasureDB;
    public MainCharacterDatabase mainCharDB;
    public DialogueDatabase dialogueDB;
    public SpeakerDatabase speakerDB;
#if UNITY_EDITOR
    private void Reset()
    {
        animalDB = FindAsset<AnimalDatabase>();
        artifactDB = FindAsset<ArtifactDatabase>();
        backgroundDB = FindAsset<BackgroundDatabase>();
        chapterDB = FindAsset<ChapterDatabase>();
        codexDB = FindAsset<CodexDatabase>();
        costumeDB = FindAsset<CostumeDatabase>();
        decoDB = FindAsset<DecoDatabase>();
        goodsDB = FindAsset<GoodsDatabase>();
        homeDB = FindAsset<HomeDatabase>();
        manualDB = FindAsset<ManualDatabase>();
        profile_iconDB = FindAsset<Profile_iconDatabase>();
        questDB = FindAsset<QuestDatabase>();
        shopDB = FindAsset<ShopDatabase>();
        shop_itemDB = FindAsset<Shop_itemDatabase>();
        stageDB = FindAsset<StageDatabase>();
        treasureDB = FindAsset<TreasureDatabase>();
        mainCharDB = FindAsset<MainCharacterDatabase>();
        dialogueDB = FindAsset<DialogueDatabase>();
        speakerDB = FindAsset<SpeakerDatabase>();

        Debug.Log("DataRegistry: 모든 DB가 자동 등록되었습니다.");
    }

    private T FindAsset<T>() where T : ScriptableObject
    {
        string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
        return null;
    }
#endif
}
