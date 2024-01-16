using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScreenMenu : MonoBehaviour
{

    public EventSystem es;
    public Button start;

    void Start() 
    {
        es.SetSelectedGameObject(null);
        es.SetSelectedGameObject(start.gameObject); // Setting selected game object not working
    }

    void Update() 
    {
        // print("current selected: " + es.currentSelectedGameObject);
    }

    public void ClickedStart() 
    {
        GameObject tmo = GameObject.Find("TransitionManager");
        tmo.GetComponent<TransitionManager>().LoadScene("PlayerMenu");
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
