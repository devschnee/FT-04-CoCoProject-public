using System;
using UnityEngine;

[Serializable]
public class QuestData
{
    public int quest_id;
    public string quest_name;
    public QuestType quest_type;
    public QuestObject quest_object;
    public string quest_obj_desc;
    public int quest_value;
    public int quest_reward_item;
    public int quest_reward_item_count;
    public string quest_desc;
}

public enum QuestType
{
    daily, weekly, achievements, daily_stackrewards, weekly_stackrewards
}
public enum QuestObject
{
    login, stage_clear, touch_animals, send_like, receive_like, visit_lobby, change_deployment, collect_cap, use_cap, collect_star, daily_quest_stack, weekly_quest_stack, connet_guest, unique_stage_clear
}