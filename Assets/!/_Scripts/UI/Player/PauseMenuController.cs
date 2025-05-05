using EMullen.MenuController;
using Unity.VisualScripting;
using UnityEngine;

public class PauseMenuController : MenuController 
{

    private float timeOpen;
    [SerializeField]
    private float minTiimeOpen = 0.1f;

    private void Update()
    {
        if(!IsOpen)
            return;


        if(Input.GetKeyDown(KeyCode.Escape) && Time.time - timeOpen > minTiimeOpen)
            Close();
            // ParentMenu.Open();
    }

    protected override void Opened()
    {
        base.Opened();
        timeOpen = Time.time;
    }   

    protected override void Closed()
    {
        base.Closed();
        (ParentMenu as PlayerHUDMenuController).ActiveHolder.alpha = 1f;
    }


}