using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NicknameChangePanel : MonoBehaviour
{
    [HideInInspector]
    public bool isNicknameChangedSuccessfully;

    [SerializeField] TMP_InputField nicknameInputField;
    [SerializeField] TMP_Text logText;
    [SerializeField] Button confirmButton;
    void Awake()
    {
        //confirmButton.onClick.AddListener(() => await OnConfirmButtonClick());
        confirmButton.onClick.AddListener(async () => await OnConfirmButtonClick());
    }

    public async Task OnConfirmButtonClick()
    {
        confirmButton.interactable = false;
        bool isSucceed = await FirebaseManager.Instance.CheckIfNameReservedAndReset(nicknameInputField.text, ShowLog);
        if (isSucceed)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() => ToMainScene());
            confirmButton.interactable = true;
        }
        else
        {
            confirmButton.interactable = true;
            nicknameInputField.text = "";
        }
    }

    private void ShowLog(string message)
    {
        if (logText.alpha < 1)
        {
            logText.alpha = 1;
        }
        logText.text = message; 
        
    }

    public void ToMainScene()
    {
        confirmButton.interactable = false;
        SceneManager.LoadScene("Main");
    }


}
