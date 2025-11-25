using UnityEngine;

public class DataManager : MonoBehaviour
{
    //Data를 초기화하고 등록, Provider 클래스들을 통해 데이터에 접근
    //데이터테이블이 추가되면 Provider 클래스를 생성하고 이곳에 등록
    public static DataManager Instance { get; private set; }
    [SerializeField] private DataRegistry dataRegistry;

    public AnimalProvider Animal { get; private set; }
    public ArtifactProvider Artifact { get; private set; }
    public BackgroundProvider Background { get; private set; }
    public ChapterProvider Chapter { get; private set; }
    public CodexProvider Codex { get; private set; }
    public CostumeProvider Costume { get; private set; }
    public DecoProvider Deco { get; private set; }
    public GoodsProvider Goods { get; private set; }
    public HomeProvider Home { get; private set; }
    public ManualProvider Manual { get; private set; }
    public Profile_iconProvider Profile { get; private set; }
    public QuestProvider Quest { get; private set; }
    public ShopProvider Shop { get; private set; }
    public Shop_itemProvider ShopItem { get; private set; }
    public StageProvider Stage { get; private set; }
    public TreasureProvider Treasure { get; private set; }
    public MainCharacterProvider mainChar { get; private set; }
    public DialogueProvider Dialogue { get; private set; }
    public SpeakerProvider Speaker { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        var loader = new ResourcesLoader();

        Animal = new AnimalProvider(dataRegistry.animalDB, loader);

        Artifact = new ArtifactProvider(dataRegistry.artifactDB, loader);

        Background = new BackgroundProvider(dataRegistry.backgroundDB, loader);

        Chapter = new ChapterProvider(dataRegistry.chapterDB, loader);

        Codex = new CodexProvider(dataRegistry.codexDB, loader);

        Costume = new CostumeProvider(dataRegistry.costumeDB, loader);

        Deco = new DecoProvider(dataRegistry.decoDB, loader);

        Goods = new GoodsProvider(dataRegistry.goodsDB, loader);

        Home = new HomeProvider(dataRegistry.homeDB, loader);

        Manual = new ManualProvider(dataRegistry.manualDB, loader);

        Profile = new Profile_iconProvider(dataRegistry.profile_iconDB, loader);

        Quest = new QuestProvider(dataRegistry.questDB, loader);

        Shop = new ShopProvider(dataRegistry.shopDB, loader);

        ShopItem = new Shop_itemProvider(dataRegistry.shop_itemDB, loader);

        Stage = new StageProvider(dataRegistry.stageDB, loader);

        Treasure = new TreasureProvider(dataRegistry.treasureDB, loader);

        mainChar = new MainCharacterProvider(dataRegistry.mainCharDB, loader);

        Dialogue = new DialogueProvider(dataRegistry.dialogueDB, loader);

        Speaker = new SpeakerProvider(dataRegistry.speakerDB, loader);
    }
}