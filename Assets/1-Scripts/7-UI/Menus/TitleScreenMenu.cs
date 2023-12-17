using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreenMenu : MonoBehaviour
{

    public void ClickedStart() 
    {
        SceneManager.LoadScene("MapSelect");
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
