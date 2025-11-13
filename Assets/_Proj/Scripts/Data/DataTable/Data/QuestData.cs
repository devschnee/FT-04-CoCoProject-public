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
    daily, weekly, achievements
}
public enum QuestObject
{
    login, pat_animal, stage_clear, get_like
}