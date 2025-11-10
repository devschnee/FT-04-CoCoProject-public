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
    public Profile_iconDatabase profile_iconDB;
    public QuestDatabase questDB;
    public ShopDatabase shopDB;
    public Shop_itemDatabase shop_itemDB;
    public StageDatabase stageDB;
    public TreasureDatabase treasureDB;
    public MainCharacterDatabase mainCharDB;
    public DialogueDatabase dialogueDB;
    public SpeakerDatabase speakerDB;
}
