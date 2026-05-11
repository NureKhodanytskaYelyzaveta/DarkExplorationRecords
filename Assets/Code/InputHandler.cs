using UnityEngine;
using UnityEngine.EventSystems;

public class InputHandler : MonoBehaviour
{
    public DialogueUI dialogueUI;
    public Yarn.Unity.DialogueRunner dialogueRunner;

    void Update()
    {
        bool tapped = false;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            if (!EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                tapped = true;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0)) tapped = true;
#endif

        if (!tapped) return;
        if (dialogueUI != null) dialogueUI.OnScreenTap();
    }
}