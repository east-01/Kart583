using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour
{
    public Animator transition;
    public GameObject child;
    public float transitionTime = 0.5f;
    
    void Awake() 
    {
        child.SetActive(true);
    }

    public void LoadScene(String sceneName) 
    {
        StartCoroutine(LoadWithAnimation(sceneName));
    }

    IEnumerator LoadWithAnimation(String sceneName) 
    {
        transition.SetTrigger("Start");

        yield return new WaitForSeconds(transitionTime);

        SceneManager.LoadScene(sceneName);
    }
    
}
