using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


public static class UserDataExtensions
{
    public static string ToJson(this IUserData data) => JsonConvert.SerializeObject(data);
    public static T FromJson<T>(this string json) where T : IUserData => JsonConvert.DeserializeObject<T>(json);
    public async static void Save(this IUserDataCategory category) => await FirebaseManager.Instance.UpdateLocalUserDataCategory(category);


}

public interface IUserData
{

}

public interface IUserDataCategory : IUserData
{
    public virtual string ToValidFormat() { throw new NotImplementedException("정의되지 않은 메서드입니다. 올바른 방법으로 사용해 주세요."); }
    
}


[Flags]
public enum UserDataDirtyFlag
{
    None,
    Master = 1,
    Wallet = 1 << 1,
    Inventory = 1 << 2,
    Lobby = 1 << 3,
    EventArchive = 1 << 4,
    Friends = 1 << 5,   
    Codex = 1 << 6,
    All = Master | Wallet | Inventory | Lobby | EventArchive | Friends | Codex

}

/// <summary>
/// <b>유저 데이터 관리용 클래스.</b>
/// <br>[DB 루트 노드] -> [users] -> [(uid)] : [이 클래스의 JSON]</br>
/// </summary>
[Serializable]
public class UserData : IUserData
//유저 데이터 관리용 클래스.    
{

    public UserDataDirtyFlag flag;

    
    
    #region 내부 클래스 정의. Firebase Realtime Database에 등록되는 각종 정보의 부모 격으로, 폴더라고 생각해주시면 됩니다.

    /// <summary>
    /// <b>유저 데이터 개요.</b>
    /// <br>1. 총 좋아요 개수</br>
    /// <br>2. 유저의 계정 생성일 타임스탬프</br>
    /// <br>3. 마지막 로그인 타임스탬프</br>
    /// <br>4. 마지막 활동 시간 타임스탬프(클라->DB 신호)</br>
    /// </summary>
    //프로필 정보(유저 데이터 개요)
    [Serializable]
    public class Master : IUserDataCategory
    {
        //유저의 닉네임
        public string nickName;

        //유저가 여지껏 받은 모든 좋아요 개수.
        public int totalLikes;

        ////유저의 계정 생성일 타임스탬프
        //public DateTime registeredDate;
        public long createdAt;

        ////마지막 로그인 시간 타임스탬프
        //public DateTime lastLogin;
        public long lastLoginAt;

        ////마지막 활동 시간 타임스탬프 (하트비트 보내듯이 주기적으로 DB에 업데이트 필요.)
        //public DateTime lastActive;
        public long lastActiveTime;

        public Master()
        {
            var now = DateTime.UtcNow;
            nickName = string.Empty;
            totalLikes = 0;
            createdAt = ((DateTimeOffset)now).ToUnixTimeSeconds();
            lastLoginAt = ((DateTimeOffset)now).ToUnixTimeSeconds();
            lastActiveTime = ((DateTimeOffset)now).ToUnixTimeSeconds();
        }


    }

    /// <summary>
    /// <b>유저 재화 정보</b>
    /// <br>1. 병뚜껑 (무료 재화)</br>
    /// <br>2. 코인 (유료 재화)</br>
    /// <br>3. 에너지 (행동력)</br>
    /// <br>각각의 필드 값 = 해당 재화의 총량을 의미함.</br>
    /// </summary>
    [Serializable]
    public class Wallet : IUserDataCategory
    {

        //병뚜껑 (무료 재화)
        public int cap;

        //코인 (유료 재화)
        public int coin;

        //에너지 (행동력)
        public int energy;
        
        public Wallet()
        {
            cap = 0;
            coin = 0;
            energy = 0;
        }
    }

    /// <summary>
    /// <b>유저 인벤토리 정보</b>
    /// <br>1. keyValues (TKey: 아이템의 id, TValue: 해당 아이템의 소지 개수)</br>
    /// </summary>
    [Serializable]
    public class Inventory : IUserDataCategory
    {
        [SerializeField]
        public Dictionary<string, int> items = new();

        public Inventory()
        {
            
        }
    }


    /// <summary>
    /// <b>로비에 배치한 장식물의 배치 정보</b>
    /// <br>1. keyValues (TKey: 장식물의 id, TValue: 그 장식물의 배치 정보 리스트)</br>
    /// </summary>
    [Serializable]
    public class Lobby : IUserDataCategory
    {

        /// <summary>
        /// <b>장식물의 배치 정보</b>
        /// <br>1. xPosition (장식물의 x 위치)</br>
        /// <br>2. yPosition (장식물의 y 위치)</br>
        /// <br>3. yAxisRotation (장식물의 y축 회전각)</br>
        /// </summary>
        [Serializable]
        public class PlaceInfo
        {
            public int xPosition;
            public int yPosition;
            public int yAxisRotation;

            public PlaceInfo()
            {
                xPosition = 0;
                yPosition = 0;
                yAxisRotation = 0;
            }
        }
        [SerializeField]
        public Dictionary<string, List<PlaceInfo>> props = new();

        public Lobby()
        {

        }
        /// <summary>
        /// 1) 씬에서 수집한 Placed 리스트 → Firebase 저장용 props 구조(Dictionary)로 변환
        ///    - 저장 시 사용됨 (CollectPlacedFromScene → LoadFromPlacedList → Save())
        /// </summary>
        public void PlacedListToUserDataLobby(List<PlaceableStore.Placed> placedList)
        {
            // props가 null이면 새로 만들고, 기존 내용은 항상 비움
            // 이유: 이번 저장에서 새롭게 채워야 하기 때문
            props ??= new Dictionary<string, List<PlaceInfo>>();
            props.Clear();

            if (placedList == null) return;

            foreach (var p in placedList)
            {
                // key는 id(문자열) — ex: "10001"
                string key = p.id.ToString();

                // key가 이미 있으면 기존 리스트 가져오고, 없으면 새 리스트 생성
                if (!props.TryGetValue(key, out var list))
                {
                    list = new List<PlaceInfo>();
                    props.Add(key, list);
                }

                // Placed → PlaceInfo 구조로 변환
                // x,z 좌표 + Y축 회전만 추출 (Firebase 저장 최소 데이터)
                var pi = new PlaceInfo
                {
                    xPosition = Mathf.RoundToInt(p.pos.x),
                    yPosition = Mathf.RoundToInt(p.pos.z),
                    yAxisRotation = Mathf.RoundToInt(p.rot.eulerAngles.y)
                };

                // 동일 ID 아래에 여러 배치물이 들어갈 수 있음 → 리스트에 추가
                list.Add(pi);
            }
        }


        /// <summary>
        /// 2) Firebase에서 내려온 props(Dictionary) → 게임에서 사용할 Placed 리스트로 변환
        ///    - 불러오기 시 사용됨 (props → ToPlacedList → SpawnFromPlacedList)
        /// </summary>
        public List<PlaceableStore.Placed> ToPlacedList()
        {
            var result = new List<PlaceableStore.Placed>();

            foreach (var kv in props)
            {
                // key(string) → id(int)
                if (!int.TryParse(kv.Key, out int idInt))
                {
                    Debug.LogWarning($"[Lobby] 잘못된 id key: {kv.Key}");
                    continue;
                }

                // ID 범위로 카테고리
                // ID 규칙(10000~, 30000~, 40000~)
                PlaceableCategory cat;
                if (10000 < idInt && idInt < 20000) cat = PlaceableCategory.Deco;
                else if (30000 < idInt && idInt < 40000) cat = PlaceableCategory.Animal;
                else if (40000 < idInt && idInt < 50000) cat = PlaceableCategory.Home;
                else
                {
                    Debug.LogWarning($"[Lobby] 지원 안 되는 id 범위: {idInt}");
                    continue;
                }

                var list = kv.Value;
                if (list == null || list.Count == 0) continue;

                // PlaceInfo → Placed 변환 반복
                foreach (var pi in list)
                {
                    var placed = new PlaceableStore.Placed
                    {
                        cat = cat,
                        id = idInt,
                        pos = new Vector3(pi.xPosition, 0f, pi.yPosition),
                        rot = Quaternion.Euler(0f, pi.yAxisRotation, 0f)
                    };

                    result.Add(placed);
                }
            }

            return result;
        }
        /// <summary>
        /// 3) Firebase에 업로드할 최종 JSON 생성
        ///    - 내부적으로 props → Placed 리스트를 만들고 직렬화
        ///    - Firebase에서 받을 때도 동일 Placed 포맷을 사용해 양쪽 구조를 맞춤
        /// </summary>
        public string ToValidFormat()
        {
            // props → Placed 리스트 변환
            var list = ToPlacedList();

            // Firebase에는 이 JSON 문자열이 그대로 저장됨
            return JsonConvert.SerializeObject(list);

            //string resultJson = string.Empty;
            //List<PlaceableStore.Placed> Wrapper = new();
            //PlaceableCategory validCategory = 0;
            //foreach (var p in props)
            //{
            //    int idInt = int.Parse(p.Key);
            //    if (10000 < idInt && idInt < 20000) validCategory = PlaceableCategory.Deco;
            //    else if (30000 < idInt && idInt < 40000) validCategory = PlaceableCategory.Animal;
            //    else if (40000 < idInt && idInt < 50000) validCategory = PlaceableCategory.Home;
            //    else return null; //심각한 예외. 저장된 string의 카테고리가 처리 가능 범위를 벗어났음.
            //    if (p.Value != null && p.Value.Count > 0)
            //    {
            //        foreach (var pi in p.Value)
            //        {
            //            var validFormat = new PlaceableStore.Placed() { cat = validCategory, id = idInt, pos = new Vector3(pi.xPosition, 0f, pi.yPosition), rot = Quaternion.Euler(0, pi.yAxisRotation, 0f) };
            //            Wrapper.Add(validFormat);
            //        }
            //    }
            //}
            //    resultJson = JsonConvert.SerializeObject(Wrapper);
            //return resultJson;
        }
    }


    /// <summary>
    /// <b>이벤트 기록</b>
    /// <br>1. keyValues (TKey: 장식물의 id, TValue: 그 장식물의 배치 정보 리스트)</br>
    /// </summary>
    [Serializable]
    public class EventArchive : IUserDataCategory
    {
        [SerializeField]
        public Dictionary<string, int> eventList = new();

        public EventArchive()
        {
            
        }
    }

    /// <summary>
    /// <b>친구 목록</b>
    /// <br>1. keyValues (TKey: 친구의 uid, TValue: 해당 친구의 친구목록 상태와 요청 시간)</br>
    /// </summary>
    [Serializable]
    public class Friends : IUserDataCategory
    {
        [SerializeField]
        public Dictionary<string, FriendInfo> friendList = new();

        /// <summary>
        /// <b>친구 상세정보</b>
        /// <br>1. 친구 상태 (0: 친구, 1: 보낸 요청, 2: 받은 요청)</br>
        /// <br>2. 요청을 보낸 시간</br>
        /// </summary>
        [Serializable]
        public class FriendInfo
        {
            
            public enum FriendState
            {
                Friend, RequestSent, RequestReceived,
            }

            public FriendState state;
            public long requestTime;

            
            public FriendInfo()
            {
                var now = DateTime.UtcNow;
                state = (FriendState)0;
                requestTime = ((DateTimeOffset)now).ToUnixTimeSeconds();
            }

        }

        public Friends()
        {

        }
    }

    [Serializable]
    public class Codex : IUserDataCategory
    {
        

        public Dictionary<string, HashSet<int>> categories = new();


        public Codex()
        {

        }
        //TODO: 수정할 것.
        public Dictionary<CodexType, HashSet<int>> LoadUnlocked()
        {
            var result = new Dictionary<CodexType, HashSet<int>>();

            foreach (var categoryKV in categories)
            {
                if (Enum.TryParse<CodexType>(categoryKV.Key, out var resultEnum))
                {
                    result.Add(resultEnum, categoryKV.Value);
                }
                else
                {
                    Debug.LogWarning("?????머임");
                }
                
            }
            return result;

        }




        [Obsolete("아몰랑")]
        public void SaveUnlocked()
        {

            // 여기서 Firebase로 업로드
            string codexString = this.ToJson();
            Debug.Log("[FirebaseCodexProgressStore] Codex progress saved (test)");
        }
    }

    //스테이지 진행상황 자료형 추가.
    //TODO: 문서화주석 만들기
    [Serializable]
    public class Progress : IUserDataCategory
    {
        [SerializeField]
        public Dictionary<string,Score> scores;

        [Serializable]
            public class Score
        {
            public bool star_1;
            public bool star_2;
            public bool star_3;
            
        }


        public Progress()
        {
            scores = new Dictionary<string,Score>();
        }

        public Dictionary<string,StageProgressData> ToStageProgressDataDictionary()
        {
            Dictionary<string,StageProgressData> dataDict = new();
            foreach (var kvp in scores)
            {
                bool[] collected = new bool[3] {kvp.Value.star_1, kvp.Value.star_2, kvp.Value.star_3};
                int bestTreasureCount = 0;
                if (kvp.Value.star_1) bestTreasureCount++;
                if (kvp.Value.star_2) bestTreasureCount++;
                if (kvp.Value.star_3) bestTreasureCount++;
                if (!dataDict.ContainsKey(kvp.Key))
                    dataDict.Add(kvp.Key, new() { stageId = kvp.Key, treasureCollected = collected, bestTreasureCount = bestTreasureCount });
            }
            return dataDict;
        }

        [Obsolete("수정중")]
        public string ToStageProgressDataWrapperJson()
        {
            var list = new List<StageProgressData>();
            foreach (var p in scores)
            {
                string stageId = p.Key;

                foreach (var s in scores.Values)
                {
                    bool[] stars = new bool[3];
                    
                        stars[0] = s.star_1;
                        stars[1] = s.star_2;
                        stars[2] = s.star_3;
                        list.Add(new() { stageId = stageId, treasureCollected = stars });
                    
                }
            }
            return JsonConvert.SerializeObject(list);

        }
    }


    #endregion


    //로컬의 UserData. Firebase DB에서 받아오게 될 것임.
    public static UserData Local { get; private set; }


    public Master master;
    public Wallet wallet;
    public Inventory inventory;
    public Lobby lobby;
    public EventArchive eventArchive;
    public Friends friends;
    public Codex codex;
    public Progress progress;
    
    public UserData()
    {
        master = new Master();
        wallet = new Wallet();
        inventory = new Inventory();
        lobby = new Lobby();
        eventArchive = new EventArchive();
        friends = new Friends();
        progress = new Progress();
        flag = 0;
    }

    //로비 배치 정보
    //(가칭)ItemPlaceInfo itemId { int id = ###, Vector2 xyPos = { ###, ### }, int rotation = ###(0~270, 90도씩 스냅) }

    //시즌별(이벤트별?) 좋아요 갯수


    //유저 도감 테이블 개요
    //도감 해금 정보(Dictionary<CodexType,HashSet<string>>) => 수하씨가 만듦 => 도감타입별로 어떤어떤 녀석들을 해금했는가?(이건 갯수가 아님)

    //유저 스테이지 데이터 개요
    //StageProgressData[] progressDatas

    //StageProgressData => public string stageId; //이 스테이지의 id;
    //                     public bool[] treasureCollected = new bool[3]; // 각 보물별 개별 획득 여부
    //                     public int bestTreasureCount = 0;              // 지금까지 달성한 최대 별 개수


    public static void SetLocal(UserData data) => Local = data;


    public static async void OnLocalUserDataUpdate()
    {
       
        await FirebaseManager.Instance.UpdateLocalUserData();
        
    }


}
