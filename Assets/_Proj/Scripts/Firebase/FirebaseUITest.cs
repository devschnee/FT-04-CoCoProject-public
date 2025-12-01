
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FirebaseUITest : MonoBehaviour
{
    //public Button testLoginButton_Anonymous;

    public TMP_InputField control_InputField;
    public TMP_Dropdown control_dropdown;

    public async void OnAnonymousLoginButtonClick()
    {
        if (!FirebaseManager.Instance && !FirebaseManager.Instance.IsInitialized) return;
        await FirebaseManager.Instance.SignInAnonymously((x) => Debug.Log($"파이어베이스 로그인 테스트: [성공]{x.UserId}로 로그인 성공"), x => Debug.LogWarning($"파이어베이스 로그인 테스트: [실패] - {x.Message}"));
        
    }

    public async void UpdateTest()
    {
        if (!FirebaseManager.Instance && !FirebaseManager.Instance.IsInitialized) return;
        if (UserData.Local == null) return;
        switch (control_dropdown.value)
        {
            case 0:
                await FirebaseManager.Instance.UploadLocalUserDataCategory(UserData.Local.inventory);
                break;
            case 1:
                await FirebaseManager.Instance.UploadLocalUserDataCategory(UserData.Local.lobby);
                break;
            case 2:
                await FirebaseManager.Instance.UploadLocalUserDataCategory(UserData.Local.eventArchive);
                break;
            case 3:
                await FirebaseManager.Instance.UploadLocalUserDataCategory(UserData.Local.friends);
                break;
        }
    }

    public void ControlUserData()
    {
        switch (control_dropdown.value)
        {
            //인벤주작
            case 0:
                if (int.TryParse(control_InputField.text, out int inventoryValue))
                {

                    if (UserData.Local.inventory.items.TryAdd(control_InputField.text, 0))
                    {
                        UserData.Local.inventory.items[control_InputField.text]++;
                    }
                    else
                    {
                        UserData.Local.inventory.items[control_InputField.text]++;
                    }
                }
                else
                {
                    Debug.LogWarning("값을 조작할 곳과 넣을 값 잘 확인해야 함");
                }
                    break;

                //배치물주작
            case 1:
                if (int.TryParse(control_InputField.text, out int propValue))
                {

                    if (UserData.Local.lobby.props.TryAdd(control_InputField.text, new() { new() { xPosition = 1, yPosition = 2, yAxisRotation = 1 } }))
                    {
                        
                    }
                    else
                    {
                        UserData.Local.lobby.props[control_InputField.text].Add( new() { xPosition = 10, yPosition = 10, yAxisRotation = 270});
                    }
                }
                else
                {
                    Debug.LogWarning("값을 조작할 곳과 넣을 값 잘 확인해야 함");
                }
                break;

                //이벤트기록주작
            case 2:
                Debug.LogWarning("이벤트기록 조작은 미구현.");
                break;

                //친구추가주작
            case 3:
                Debug.LogWarning("친구추가 조작은 미구현.");
                break;
        }
    }

}
