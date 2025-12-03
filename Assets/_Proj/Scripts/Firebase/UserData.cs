using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public enum GoodsType
{
    cap, coin, energy
}
public static class UserDataExtensions
{
    public static string ToJson(this IUserData data) => JsonConvert.SerializeObject(data);
    public static T FromJson<T>(this string json) where T : IUserData => JsonConvert.DeserializeObject<T>(json); 
    public async static void Save(this IUserDataCategory category) => await FirebaseManager.Instance.UploadLocalUserDataCategory(category);
    public async static void Save(this UserData data) => await FirebaseManager.Instance.UpdateLocalUserData();


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
    Goods = 1 << 1,
    Inventory = 1 << 2,
    Lobby = 1 << 3,
    EventArchive = 1 << 4,
    Friends = 1 << 5,
    Codex = 1 << 6,
    Quest = 1 << 7,
    All = Master | Goods | Inventory | Lobby | EventArchive | Friends | Codex | Quest

}

/// <summary>
/// <b>유저 데이터 관리용 클래스.</b>
/// <br>[DB 루트 노드] -> [users] -> [(uid)] : [이 클래스의 JSON]</br>
/// </summary>
[Serializable]
public class UserData : IUserData
//유저 데이터 관리용 클래스.    
{





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

        public long lastEnergyTime;

        //프로필에 표시될 아이콘, 프로필에 표시될 애장품 목록.
        public Dictionary<string, int> profile = new();
        public Master()
        {
            var now = DateTime.UtcNow;
            nickName = string.Empty;
            totalLikes = 0;
            createdAt = ((DateTimeOffset)now).ToUnixTimeSeconds();
            lastLoginAt = ((DateTimeOffset)now).ToUnixTimeSeconds();
            lastActiveTime = ((DateTimeOffset)now).ToUnixTimeSeconds();
            lastEnergyTime = ((DateTimeOffset)now).ToUnixTimeSeconds();

            string[] enums = Enum.GetNames(typeof(ProfileType));
            for (int i = 0; i < enums.Length; i++)
            {
                profile.TryAdd(enums[i].ToString().ToLower(), -1);
            }
            profile["icon"] = 120001;
        }

        public int this[ProfileType type]
        {
            get
            {
                string enumString = type.ToString().ToLower();
                return profile.TryGetValue(enumString, out var value) ? value : -1;
            }

            set
            {
                string enumString = type.ToString().ToLower();
                if (!profile.ContainsKey(enumString))
                    profile.Add(enumString, value);
                else
                    profile[enumString] = value;
            }
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
    public class Goods : IUserDataCategory
    {

        public Dictionary<string, int> values;
        ////병뚜껑 (무료 재화)
        //public int cap;

        ////코인 (유료 재화)
        //public int coin;

        ////에너지 (행동력)
        //public int energy;

        [NonSerialized]
        public Action onValueChanged;

        public int this[int goodsId]
        {
            get => goodsId == 110001 ? values["energy"] : goodsId == 110002 ? values["cap"] : values["coin"];
            set
            {
                var targetKey = goodsId == 110001 ? "energy" : goodsId == 110002 ? "cap" : "coin";
                values[targetKey] = value;
                onValueChanged?.Invoke();
            }
        }

        public int this[GoodsType type]
        {
            get => type == GoodsType.energy ? this[110001] : type == GoodsType.cap ? this[110002] : this[110003];
            set => values[type.ToString().ToLower()] = value;

        }

        public Goods()
        {
            values = new();
            values.Add(GoodsType.cap.ToString(), 0);
            values.Add(GoodsType.coin.ToString(), 0);
            values.Add(GoodsType.energy.ToString(), 5);
        }
    }

    /// <summary>
    /// <b>유저 인벤토리 정보</b>
    /// <br>1. keyValues (TKey: 아이템의 id, TValue: 해당 아이템의 소지 개수)</br>
    /// </summary>
    [Serializable]
    public class Inventory : IUserDataCategory
    {
        //전체 목록에서, 어떤 아이템(string으로 저장된 id)인지, 몇 개나(value)있는지?
        [SerializeField]
        public Dictionary<string, int> items = new();




        public void Add(int id)
        {
            if (items.ContainsKey(id.ToString())) items[id.ToString()]++;
            else items.Add(id.ToString(), 1);
            this.Save();
        }



        public bool Get(int id, out object resultItem)
        {
            resultItem = null;
            if (items.ContainsKey(id.ToString()))
            {
                if (10000 < id && id < 20000)
                    resultItem = DataManager.Instance.Deco.GetData(id);
                if (30000 < id && id < 40000)
                    resultItem = DataManager.Instance.Animal.GetData(id);



                return true;
            }
            else
            {

                return false;

            }


        }
        public int this[int id]
        {

            get => items.TryGetValue(id.ToString(), out int value) ? value : 0;
            set
            {
                string key = id.ToString();
                // 있으면 업데이트, 없으면 추가
                if (items.ContainsKey(key))
                    items[key] = value;
                else
                    items.Add(key, value);

                //if (items.TryGetValue(id.ToString(), out int v))
                //{
                //    v = value;
                //}
                //else
                //{
                //    items.Add(id.ToString(), value);
                //}

            }
        }
        public Inventory()
        {
            //12.3mj
            // 새 계정 기본 아이템: 10001 나무 1개
            // (이미 Codex 쪽에서 10001은 MarkAlwaysUnlocked 되어 있음)
            if (items == null)
                items = new Dictionary<string, int>();

            const int defaultTreeId = 10001;
            string key = defaultTreeId.ToString();

            // 기존 데이터(디시리얼라이즈)에는 손 안 대고,
            // 정말 새로 만들 때만 기본값이 들어가도록 방어
            if (!items.ContainsKey(key))
            {
                items[key] = 1;
            }
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
    /// <br>1. keyValues (TKey: 이벤트(시즌)의 id, TValue: 해당 이벤트에서 받은 좋아요 개수)</br>
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

            }

        }

        public Friends()
        {

        }

        [NonSerialized]
        public Action onFriendsUpdate;
    }

    [Serializable]
    public class Codex : IUserDataCategory
    {

        //키: 코덱스 타입(home, artifact, animal, costume, deco),
        //아이템아이디별 코덱스 구간: 10001~19999: deco, 50001~59999: artifact, 
        //밸류: 각각의 해시셋에는 해금된 도감의 codex_id(string)가 들어가야 논리적으로 맞는데...
        //아이템아이디가 전부 할당되어 있고, 이걸 기준으로 찾아도 되니까 int로 저장하려고 했지만 아이템아이디가 중구난방임.
        //5만번대면 artifact여야 하는데 코인, 병뚜껑, 에너지는 11만번대면서 artifact임. 에너지는 또 왜 있어 이거
        //뭐 아무튼간에 이 클래스는 '코덱스 전체의 해금 여부'만을 기본적으로 제공하면 됨.
        public Dictionary<string, HashSet<int>> categories = new();
        public Dictionary<string, HashSet<int>> newlyUnlocked = new();


        public Codex()
        {
            string[] enumStrings = Enum.GetNames(typeof(CodexType));
            foreach (var enumString in enumStrings)
            {
                categories.Add(enumString.ToLower(), new HashSet<int>());
                newlyUnlocked.Add(enumString.ToLower(), new());

            }
            //12.01mj
            //  기본 제공 나무(예: deco 10001)는 "해금"만 해주고,
            //  newlyUnlocked에는 넣지 않도록 직접 categories만 건드림
            // 기본 제공 나무 알림 계속 떠서 수정
            MarkAlwaysUnlocked(CodexType.deco, 10001);
            //this[CodexType.deco, 10001] = true;
        }
        //12.01mj
        /// <summary>
        /// 시작 아이템처럼 "항상 해금되어 있어야 하지만"
        /// 새로 얻은 것으로 취급하고 싶지 않을 때 쓰는 함수
        /// (newlyUnlocked에는 넣지 않음)
        /// </summary>
        private void MarkAlwaysUnlocked(CodexType type, int itemId)
        {
            string key = type.ToString().ToLower();
            if (!categories.TryGetValue(key, out var set) || set == null)
            {
                set = new HashSet<int>();
                categories[key] = set;
            }

            set.Add(itemId);   // 🔴 여기서는 newlyUnlocked 건드리지 않음
        }
        /// <summary>
        /// 아이템아이디를 매개변수로 이 코덱스에 접근하기만 하면 해금이 되었는지 아닌지 여부를 반환.
        /// </summary>
        /// <param name="itemId">해금 여부를 검사하고 싶은 요소의 item_id</param>
        /// <returns></returns>
        public bool this[CodexType? type, int itemId]
        {
            get
            {
                if (type != null)
                {
                    Debug.Log($"UserData-Codex: {type.ToString().ToLower()}를 검사 중");
                    if (!categories.TryGetValue(type.ToString().ToLower(), out var values))
                    {
                        //아예 아무것도 없는 상황임.
                        categories.Add(type.ToString().ToLower(), new HashSet<int>());
                        return false;
                    }
                    return values.Contains(itemId);
                }
                else
                {
                    Debug.Log($"UserData-Codex: 모든 코덱스 타입을 검사 중");
                    var allCats = categories.Values;
                    foreach (var item in allCats)
                    {
                        if (item.Contains(itemId))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
            set
            {
                //12.02mj
                // false로 바꾸는 건 아직 지원 안 함
                if (!value) return;

                // 방어 코드: type이 null이면 그냥 로그 찍고 무시
                if (type == null)
                {
                    Debug.LogWarning($"[UserData.Codex] type == null 상태로 setter 호출됨 (itemId={itemId})");
                    return;
                }

                string key = type.Value.ToString().ToLower();

                // 1) categories 쪽에 등록
                if (!categories.TryGetValue(key, out var values) || values == null)
                {
                    values = new HashSet<int>();
                    categories[key] = values;
                }

                bool isNew = values.Add(itemId); // 새로 추가되면 true

                // 2) 새로 해금된 경우에만 newlyUnlocked에 추가
                if (isNew)
                {
                    if (!newlyUnlocked.TryGetValue(key, out var newValues) || newValues == null)
                    {
                        newValues = new HashSet<int>();
                        newlyUnlocked[key] = newValues;
                    }

                    newValues.Add(itemId);

                    // 🔴 여기서 한 번만 전체 빨간점 재계산
                    CodexRedDotManager.Recalculate();
                }

                // 3) Firebase 저장
                this.Save();
            }
            //if (value)
            //{
            //    bool isNew = false;
            //    if (!categories.TryGetValue(type.ToString().ToLower(), out var values))
            //    {
            //        categories.Add(type.ToString().ToLower(), new HashSet<int>());
            //        categories[type.ToString().ToLower()].Add(itemId);
            //        isNew = true;
            //    }
            //    else
            //    {
            //        isNew = !values.Contains(itemId);
            //        values.Add(itemId);
            //    }

            //    if (isNew)
            //    {
            //        if (!newlyUnlocked.TryGetValue(type.ToString().ToLower(), out var newValues))
            //        {
            //            newlyUnlocked.Add(type.ToString().ToLower(), new HashSet<int>());
            //            newlyUnlocked[type.ToString().ToLower()].Add(itemId);
            //        }
            //        else
            //        {
            //            newlyUnlocked[type.ToString().ToLower()].Add(itemId);
            //        }
            //    }




            //    this.Save();
            //}
            ////false 대입 시의 동작.. 코덱스 해금 상태 해제? 지금은 필요가 없음.
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
        public Dictionary<string, Score> scores;

        [Serializable]
        public class Score
        {
            public bool star_1;
            public bool star_2;
            public bool star_3;

            //보상지급할때 사용
            //얘네가 true면은 이미 보상을 받은것이므로 지급x
            public bool star_1_rewarded;
            public bool star_2_rewarded;
            public bool star_3_rewarded;


        }



        public Progress()
        {
            scores = new Dictionary<string, Score>();
        }

        public Dictionary<string, StageProgressData> ToStageProgressDataDictionary()
        {
            Dictionary<string, StageProgressData> dataDict = new();
            foreach (var kvp in scores)
            {
                bool[] collected = new bool[3] { kvp.Value.star_1, kvp.Value.star_2, kvp.Value.star_3 };
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

    [Serializable]
    public class Preferences : IUserDataCategory
    {

        public Preferences()
        {
            skipDialogues = false;
        }

        public bool skipDialogues;
        public void ApplyAll()
        {
            //각각의 옵션 값을 필요로 하는 객체(매니저라던가...)에게 전달.

        }
    }

    [Serializable]
    public class Quest : IUserDataCategory
    {
        /// <summary>
        /// TKey: string - 퀘스트 id(int)
        /// TValue: int - 퀘스트 진행도 
        /// </summary>
        public Dictionary<int, int> progress;    //키: quest_id, 값: 행위의 횟수. 어떤 행위인지는 중요하지 않음.
        public HashSet<int> rewarded;            //보상받은 questId

        public HashSet<int> stackRewarded;
        public long lastDailyResetAt;
        public long lastWeeklyResetAt;


        public int this[int questId]
        {
            get
            {
                return progress[questId];
            }
            set
            {
                progress[questId] = value;
            }
        }

        public Quest()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            lastDailyResetAt = now;
            lastWeeklyResetAt = now;

            progress = new Dictionary<int, int>();
            rewarded = new HashSet<int>();
            stackRewarded = new HashSet<int>();

            foreach (var x in DataManager.Instance.Quest.Database.questList)
            {
                if (!progress.TryAdd(x.quest_id, 0)) continue;
            }



        }
    }

    #endregion


    //로컬의 UserData. Firebase DB에서 받아오게 될 것임.
    public static UserData Local { get; private set; }


    public int passedTutorials;
    //예) 친구추가 오면 이 더티플래그를 더럽게 만들어줌.
    public UserDataDirtyFlag flag;

    public Master master;
    public Goods goods;
    public Inventory inventory;
    public Lobby lobby;
    public EventArchive eventArchive;
    public Friends friends;
    public Codex codex;
    public Progress progress;
    public Preferences preferences;
    public Quest quest;



    public UserData()
    {
        master = new Master();
        goods = new Goods();
        inventory = new Inventory();
        lobby = new Lobby();
        eventArchive = new EventArchive();
        friends = new Friends();
        codex = new Codex();
        progress = new Progress();
        preferences = new Preferences();
        quest = new Quest();
        flag = 0;
        passedTutorials = 0;

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


    public static void Clear() => Local = null;
    public static void SetLocal(UserData data)
    {
        Local = data;
        data.preferences.ApplyAll();
    }

    public void SetCategory(IUserDataCategory category)
    {
        switch (category)
        {
            case Master master:
                this.master = master;
                this.flag &= ~UserDataDirtyFlag.Master;
                break;
            case Goods goods:
                this.goods = goods;
                this.flag &= ~UserDataDirtyFlag.Goods;
                break;
            case Inventory inventory:
                this.inventory = inventory;
                this.flag &= ~UserDataDirtyFlag.Inventory;
                break;
            case EventArchive eventArchive:
                this.eventArchive = eventArchive;
                this.flag &= ~UserDataDirtyFlag.EventArchive;
                break;
            case Friends friends:
                friends.onFriendsUpdate = this.friends.onFriendsUpdate;
                this.friends = friends;
                this.flag &= ~UserDataDirtyFlag.Friends;
                this.friends.onFriendsUpdate?.Invoke();
                break;
            case Codex codex:
                this.codex = codex;
                this.flag &= ~UserDataDirtyFlag.Codex;
                break;
            case Quest quest:
                this.quest = quest;
                this.flag &= ~UserDataDirtyFlag.Quest;
                break;
            //case Progress progress:
            //    this.progress = progress;
            //    this.flag &= ~UserDataDirtyFlag.Progress;
            //    break;
            //case Preferences preferences:
            //    this.preferences = preferences;
            //    break;


            default:
                break;
        }

    }

    public static async void OnLocalUserDataUpdate()
    {

        await FirebaseManager.Instance.UpdateLocalUserData();

    }


}
