using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class CsvImportManager : EditorWindow
{
    [MenuItem("Tools/CSV Import Manager")]
    public static void ShowWindow() => GetWindow(typeof(CsvImportManager));
    //유니티 에디터 상에서 Tools -> CSV Import Manager 생성

    private string csvFolderPath = "Assets/_Proj/Data/CSV";
    private string metaFilePath = "Assets/_Proj/Scripts/Editor/Tools/table_meta.json";
    //각각 CSV를 보관하는 폴더의 경로와 json이 보관된 경로
    //경로가 변경될시 이곳에서 바꿔줘야함
    
    void OnGUI()
    {
        if (GUILayout.Button("CSV → ScriptableObject 변환"))
        {
            ImportAllTables();
            //에디터에 생성한 CSV Import Manager의 버튼 생성 및 수행할 함수 할당
        }
    }

    void ImportAllTables()
    {
        // JsonUtility로 메타 읽기
        string metaJson = File.ReadAllText(metaFilePath);
        //텍스트 파일을 열고, 파일의 모든 텍스트를 읽은 다음에 파일을 닫고
        var metaList = JsonUtility.FromJson<TableMetaList>(metaJson);
        //entries json으로 변환

        foreach (string csvPath in Directory.GetFiles(csvFolderPath, "*.csv"))
        {
            //미리 경로를 저장해둔 csvFolderPath 를 매개변수로 사용하고, 해당 경로에서
            //"*.csv"의 조건에 맞는 파일을 탐색한다.
            //* 은 앞에 올 파일이름명들 총칭, .csv는 확장자명
            //xxxx.csv와 같은 형식이라면 모두 탐색, 여기서 xxxx을 *로 처리한것
            //->지정된 디렉터리에서 지정된 검색 패턴과 일치하는 파일 이름(파일 경로 포함)을 반환

            string fileName = Path.GetFileNameWithoutExtension(csvPath);
            //확장명 없이 지정된 경로 문자열의 파일 이름을 반환

            // meta에 존재하는지 확인
            var meta = metaList.entries.Find(x => x.name == fileName);
            //metaList에 저장된 name변수와 같은 이름의 fileName이 있는지 체크
            if (meta == null)
            {
                Debug.Log($"[CSV] {fileName} 은 meta.json에 정의되어 있지 않음 → 건너뜀");
                continue;
            }

            if (meta.type != "SO") continue;

            //type이 "SO"라면 테이블 이름별 전용 Parser 호출
            switch (fileName)
            {
                //CSV의 type이 "SO"인 경우가 추가될 때마다 case를 추가, 각 CSV에 맞는 Parser클래스 생성
                case "tbl_animal_mst":
                    AnimalParser.Import(csvPath);
                    break;
                case "tbl_artifact_mst":
                    ArtifactParser.Import(csvPath);
                    break;
                case "tbl_background_mst":
                    BackgroundParser.Import(csvPath);
                    break;
                case "tbl_chapter_mst":
                    ChapterParser.Import(csvPath);
                    break;
                case "tbl_codex_mst":
                    CodexParser.Import(csvPath);
                    break;
                case "tbl_costume_mst":
                    CostumeParser.Import(csvPath);
                    break;
                case "tbl_deco_mst":
                    DecoParser.Import(csvPath);
                    break;
                case "tbl_dialogue_mst":
                    DialogueParser.Import(csvPath);
                    break;
                case "tbl_dialogue_speakers_dtl":
                    SpeakerParser.Import(csvPath);
                    break;
                case "tbl_goods_mst":
                    GoodsParser.Import(csvPath);
                    break;
                case "tbl_home_mst":
                    HomeParser.Import(csvPath);
                    break;
                case "tbl_manual_mst":
                    ManualParser.Import(csvPath);
                    break;
                case "tbl_profile_icon_mst":
                    Profile_iconParser.Import(csvPath);
                    break;
                case "tbl_quest_mst":
                    QuestParser.Import(csvPath);
                    break;
                case "tbl_shop_item_dtl":
                    Shop_itemParser.Import(csvPath);
                    break;
                case "tbl_shop_mst":
                    ShopParser.Import(csvPath);
                    break;
                case "tbl_stage_mst":
                    StageParser.Import(csvPath);
                    break;
                case "tbl_treasure_mst":
                    TreasureParser.Import(csvPath);
                    break;
                default:
                    Debug.LogWarning($"[CSV] {fileName} 변환 로직 없음");
                    break;
            }
        }
    }
}