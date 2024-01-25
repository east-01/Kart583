using UnityEngine;

public class MenuTitleController : MonoBehaviour
{
    public TitleShipFlight titleShip;

    public void ClickedStart() 
    {
        GameObject tmo = GameObject.Find("TransitionManager");
        tmo.GetComponent<TransitionManager>().LoadScene(SceneNames.MENU_PLAYER);
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
}
