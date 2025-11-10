using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DialogueDatabase", menuName = "GameData/DialogueDatabase")]
public class DialogueDatabase : ScriptableObject
{
    public List<DialogueData> dialogueList = new List<DialogueData>();
}