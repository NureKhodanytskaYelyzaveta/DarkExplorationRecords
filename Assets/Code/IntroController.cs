using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class IntroController : MonoBehaviour
{
    void Update()
    {
        bool tapped = false;

        if (Input.touchCount > 0 &&
            Input.GetTouch(0).phase == TouchPhase.Began)
            tapped = true;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0)) tapped = true;
#endif

        if (tapped)
        {
            LoadingScreen.LoadScene("MainMenuScene");
        }
    }
}