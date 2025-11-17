using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameInfoPopup : MonoBehaviour
{
    [SerializeField] private Button[] tabButtons;
    [SerializeField] private TMP_Text contentText;
    //[SerializeField] private GameInfoDB dataDB;

    private int currentTab = 0;

    public void Open()
    {
        gameObject.SetActive(true);
        ChangeTab(0);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void ChangeTab(int index)
    {
        currentTab = index;
        //contentText.text = dataDB.infos[index].content;

        for (int i = 0; i < tabButtons.Length; i++)
        {
            var colors = tabButtons[i].colors;
            colors.normalColor = (i == index) ? Color.white : Color.gray;
            tabButtons[i].colors = colors;
        }
    }
}
