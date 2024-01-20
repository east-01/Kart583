using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/** Responsible for loading a LevelDataPackage onto the level icon. 
    Will load images, text, etc. */
public class LevelIcon : MonoBehaviour
{

    public GameObject mapImageObj;
    public TMP_Text titleText;

    private LevelDataPackage data;

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
        int buildIndex = SceneUtility.GetBuildIndexByScenePath(data.sceneName);
        if(buildIndex == -1) 
            throw new InvalidOperationException("Scene \"" + data.sceneName + "\" doesn't exist (or doesn't have a build index at least)");
        
        GameObject tmo = GameObject.Find("TransitionManager");
        tmo.GetComponent<TransitionManager>().LoadScene(data.sceneName);
    }

}
