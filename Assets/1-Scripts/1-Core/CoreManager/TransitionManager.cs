using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour
{

    /* Fade in/out related objects */
    public Animator transition;
    public GameObject child;
    public float transitionTime = 0.5f;
    
    private Animator menuTransition;

    /* Between scene data */
    private string precedingScene;
    private Quaternion rotation;
    private Vector3 rotVector;

    void Awake() 
    {
        child.SetActive(true);

        // Attempt to find menu transition
        if(SceneManager.GetActiveScene().name.StartsWith("Menu")) {
            GameObject mco = GameObject.Find("MenuCanvas");
            menuTransition = mco.GetComponent<Animator>();

            GameObject menuCamera = GameObject.Find("MenuCamera");
            menuCamera.transform.rotation = rotation;
            MenuCameraDrift mcd = menuCamera.GetComponent<MenuCameraDrift>();
            mcd.SetRotationVector(rotVector);
            mcd.SetRotationVectorSelectTime(Random.Range(15, 25));
        }

        /* Wake up animation */
        if(precedingScene != null && !precedingScene.StartsWith("Menu") ||
           precedingScene == SceneNames.MENU_MAP) {
            transition.SetTrigger("FadeFromBlack");
        }

    }

    public void LoadScene(string sceneName) 
    {
        StartCoroutine(LoadWithAnimation(sceneName));
    }

    IEnumerator LoadWithAnimation(string sceneName) 
    {

        if(SceneManager.GetActiveScene().name == SceneNames.MENU_TITLE) {
            GameObject.Find("MenuCanvas").GetComponent<MenuTitleController>().titleShip.fly = true;
        }

        bool isMenuTransition = SceneManager.GetActiveScene().name.StartsWith("Menu") && sceneName.StartsWith("Menu");
        if(isMenuTransition) {
            menuTransition.SetTrigger("Animate");
            yield return new WaitForSeconds(0.5f);
        } else {
            transition.SetTrigger("FadeToBlack");
            yield return new WaitForSeconds(transitionTime);
        }

        // Set between transition fields
        precedingScene = SceneManager.GetActiveScene().name;
        if(SceneManager.GetActiveScene().name.StartsWith("Menu")) {
            GameObject menuCamera = GameObject.Find("MenuCamera");
            rotation = menuCamera.transform.rotation;
            rotVector = menuCamera.GetComponent<MenuCameraDrift>().GetRotationVector();
        }

        SceneManager.LoadScene(sceneName);
    }
    
}
