using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public void OnPlayButtonClick()
    {
        LoadingScreen.LoadScene("SampleScene");
    }
}