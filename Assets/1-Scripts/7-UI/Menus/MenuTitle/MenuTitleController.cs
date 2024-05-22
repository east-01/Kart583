using UnityEngine;
using UnityEngine.UI;

public class MenuTitleController : MenuController
{
    [SerializeField] private AudioClip menuAmbiance;

    public TitleShipFlight titleShip;

    protected new void Awake() 
    {
        base.Awake();
        CoreManager.AudioManager.PlaySound(menuAmbiance, 1f, true);
    }

    public void ClickedStart(bool isMultiplayer) 
    {
        CoreManager.Instance.isMultiplayer = isMultiplayer;
        CoreManager.TransitionManager.LoadScene(SceneNames.MENU_PLAYER);
    }

    public void ClickedOptions() 
    {
        print("TODO: Create options menu");
        // SceneManager.LoadScene("OptionsMenu");
    }

    public void ClickedQuit() 
    {
        Application.Quit();
    }

    protected override void SendMenuBack()
    {
        
    }
}
