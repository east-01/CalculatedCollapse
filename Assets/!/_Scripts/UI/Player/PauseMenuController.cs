using EMullen.Core;
using EMullen.MenuController;
using EMullen.Networking;
using EMullen.SceneMgmt;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuController : MenuController, IInputListener
{

    private float timeOpen;
    [SerializeField]
    private float minTimeOpen = 0.1f;

    private void Update()
    {
        if(!IsOpen)
            return;

        // if(Input.GetKeyDown(KeyCode.Escape) && Time.time - timeOpen > minTimeOpen)
        //     SendMenuBack();
    }

    protected override void Opened()
    {
        base.Opened();
        timeOpen = Time.time;
    }   

    public void QuitPressed() 
    {
        NetworkController.Instance.StopNetwork();
        BLog.Highlight("TODO: Title screen");
    }

    public void ResumePressed() => Exit();

    private void Exit() 
    {
        SendMenuBack();
        (ParentMenu as PlayerHUDMenuController).Shown();
    }

    public void InputEvent(InputAction.CallbackContext context)
    {
        switch(context.action.name) 
        {
            case "Pause":
                if(context.performed && IsOpen) {
                    Exit();
                }
                break;
        }
    }

    public void InputPoll(InputAction action) {}
}