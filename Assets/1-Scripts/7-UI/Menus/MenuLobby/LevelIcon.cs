using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/** Responsible for loading a LevelDataPackage onto the level icon. 
    Will load images, text, etc. */
public class LevelIcon : MonoBehaviour
{

    private MenuMapController menuMapController;

    [SerializeField] private GameObject mapImageObj;
    [SerializeField] private TMP_Text titleText;

    private LevelDataPackage data;

    private void Awake() 
    {
        MenuMapController[] menuMapControllers = FindObjectsOfType<MenuMapController>();
        if(menuMapControllers.Length != 1) {
            Debug.LogError($"Located more than one MenuMapController ({menuMapControllers.Length})");
            gameObject.SetActive(false);
            return;
        }

        menuMapController = menuMapControllers[0];
    }

    /** Load a LevelDataPackage and update visuals. */
    public void Load(LevelDataPackage data) 
    {
        this.data = data;

        mapImageObj.GetComponent<Image>().sprite = data.levelImage;
        titleText.text = data.levelString;
    }

    public void SetBorder(Color color) 
    {
        gameObject.GetComponent<Image>().color = color;
    }

    public void Clicked() 
    {
        menuMapController.ClickedMapIcon(data.Level);
    }

}
