using Game.Inventory;
using System.Collections.Generic;
using UnityEngine;


public interface IQuestBehaviour
{
    //public virtual void Handle(object value = null)
    //{
    //    QuestManager.Instance.Handle(this, value);
    //}
}
public class QuestManager : MonoBehaviour
{
    #region Defining Singleton
    public static QuestManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this && Instance.gameObject != gameObject)
        { 
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(gameObject);

    }
    #endregion
    void Start()
    {
        
       
    }
    public void Handle(IQuestBehaviour behaviour, object value = null)
    {
        //daily, weekly, achievements, daily_stackrewards, weekly_stackrewards
        
        //11. 27. 기준 당장 구현할 수 있는 퀘스트 조건
        //login, stage_clear, touch_animals, change_deployment, collect_cap, use_cap, collect_star, daily_quest_stack, weekly_quest_stack, connect_guest, unique_stage_clear 딱 여기까지.

        //퀘스트 조건별 이 메서드를 호출해줘야 하는 클래스
        //login, connect_guest: FirebaseManager 됨.
        //stage_clear, collect_star: stageManager 됨.
        //touch_animals: 로비 터치 핸들링하는거 아무거나 됨.
        //change_deployment: 로비 저장하는거가 해주면 됨 됨.
        //collect_cap, use_cap: 처리됨. 재화서비스가 해주면 됨. 됨
        //quest_stack류: 수하씨가 만든 퀘스트 보상 처리가 해주면 됨.
        //unique_stage_clear: 이건 처리됨. 프로그레스매니저가 해줌. 됨
        //visit_lobby: 로비 방문 됨.
        
            List<QuestData> dataList = new();
        switch (behaviour)
        {
            case StageManager stageManager:
                dataList =
                    DataManager.Instance.Quest.Database.questList.FindAll
                    (x => x.quest_object == QuestObject.stage_clear ||
                     
                     x.quest_object == QuestObject.collect_star);
                foreach (var d in dataList)
                {
                    if (d.quest_object == QuestObject.stage_clear)
                    {
                        UserData.Local.quest.progress[d.quest_id]++;
                    }
                    
                    if (d.quest_object == QuestObject.collect_star)
                    {
                        UserData.Local.quest.progress[d.quest_id] += (int)value;
                    }
                }
                break;
                    
                
                
                //스테이지 관련 퀘스트 진행도 기록
                //quest_object가 stage_clear인 퀘스트
                //quest_object가 collect_star인 퀘스트


                case PlayerProgressManager progressManager:
                dataList =
                DataManager.Instance.Quest.Database.questList.FindAll
                (x=>x.quest_object == QuestObject.unique_stage_clear);


                foreach (var d in dataList)
                {
                    
                        UserData.Local.quest.progress[d.quest_id]++;
                    
                }
                break;

            case GoodsService goodsService:
                if ((int)value < 0)
                {
                    dataList =
                    DataManager.Instance.Quest.Database.questList.FindAll(x => x.quest_object == QuestObject.use_cap);
                    foreach (var d in dataList)
                    {
                        UserData.Local.quest.progress[d.quest_id] -= (int)value;
                    }
                }
                else
                {
                    dataList =
                    DataManager.Instance.Quest.Database.questList.FindAll(x => x.quest_object == QuestObject.collect_cap);
                    foreach (var d in dataList)
                    {
                        UserData.Local.quest.progress[d.quest_id] += (int)value;
                    }
                }   
                    break;

            case AnimalBehaviour animalBehaviour:
                {
                    dataList =
                    DataManager.Instance.Quest.Database.questList.FindAll(x => x.quest_object == QuestObject.touch_animals);
                    foreach (var d in dataList)
                    {
                        UserData.Local.quest.progress[d.quest_id] ++;
                    }
                }
                break;

                //TODO: 일일퀘스트, 주간퀘스트의 경우 보상을 받는 시점에 핸들링되도록 잘 변경.
        case QuestPanelController questPanelController:
            {
                    if ((QuestType)value == QuestType.daily)
                    dataList =
                    DataManager.Instance.Quest.Database.questList.FindAll(x => x.quest_object == QuestObject.daily_quest_stack);
                    if ((QuestType)value == QuestType.weekly)
                    dataList =
                    DataManager.Instance.Quest.Database.questList.FindAll(x => x.quest_object == QuestObject.weekly_quest_stack);
                    foreach (var d in dataList)
                    {
                        UserData.Local.quest.progress[d.quest_id]++;
                    }
                }
                break;
        case EditModeController editModeController:
            {
                    dataList =
                        DataManager.Instance.Quest.Database.questList.FindAll(x => x.quest_object == QuestObject.change_deployment);
                    foreach (var d in dataList)
                    {
                        UserData.Local.quest.progress[d.quest_id]++;
                    }
                }
            break;
        case FirebaseManager firebaseManager:
            {
                    if ((string)value == "Login")
                    
                        dataList =
                            DataManager.Instance.Quest.Database.questList.FindAll(x => x.quest_object == QuestObject.login);
                    

                    if ((string)value == "GoogleLogin")
                        dataList =
                            DataManager.Instance.Quest.Database.questList.FindAll(x => x.quest_object == QuestObject.login);
                    
                    foreach (var d in dataList)
                    {
                        if ((string)value != "GoogleLogin")
                        {
                            if (d.quest_type == QuestType.daily)
                            {
                                UserData.Local.quest.progress[d.quest_id]++;
                            }
                            else if (d.quest_type == QuestType.weekly)
                            {
                                //TODO: 주간 로그인인 경우, 하루에 한 번만 로그인 횟수 늘려주는 처리가 필요함.
                                //UserData.Local.master의 lastLoginAt을 가지고 판단해야 함. 한국시간 오전 6시 기준으로 해야 함.
                                //1) 현재 시간과 마지막 로그인 시간의 차를 구함.
                                //2) 현재 시간과 마지막 로그인 시간의 차가 24시간이 넘어간다면 반드시 하나 올려줌.
                                //3) 만약 24시간 이내라면, 마지막 로그인 시간이 한국시간 기준으로 오전 6시 전이었는지 후였는지 알아내야 함.
                                //4-1) 만약 마지막 로그인 시간이 오전 6시 전이었다면 아래 코드를 실행해줌.
                                //4-2) 만약 마지막 로그인 시간이 오전 6시 이후였다면 아래 코드를 실행하지 않음.
                                UserData.Local.quest.progress[d.quest_id]++;
                            }
                        }
                        else
                        {
                            UserData.Local.quest.progress[d.quest_id] = 1;
                        }
                    }
                }
            break;
            case FriendLobbyManager fm:
                dataList =
                        DataManager.Instance.Quest.Database.questList.FindAll(x => x.quest_object == QuestObject.visit_lobby);
                foreach (var d in dataList)
                {
                    UserData.Local.quest.progress[d.quest_id]++;
                }

                break;

        }

        UserData.Local.quest.Save();

        //12.01mj
        //  퀘스트 진행도 바뀔 때마다 빨간 점 다시 계산
        QuestRedDotManager.Recalculate();
    }
}
