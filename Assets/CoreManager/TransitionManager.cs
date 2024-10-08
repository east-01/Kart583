using System.Collections;
using EMullen.SceneMgmt;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour
{

    /* Fade in/out related objects */
    public Animator transition;
    public GameObject child;
    public float transitionTime = 0.5f;
    
    /* Between scene data */
    private string precedingScene;
    private Quaternion rotation;
    private Vector3 rotVector;

    void Awake() 
    {
        child.SetActive(true);

        /* Wake up animation */
        if(precedingScene != null && !precedingScene.StartsWith("Menu")) {
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
            FindMenuObjects().Item1.SetTrigger("Animate");
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

        if(SceneController.Instance != null)
            SceneController.Instance.LoadScene(new(sceneName), false);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
    
    /// <summary>
    /// Find the objects related to the menu.
    /// </summary>
    /// <returns>The menu transition animator and the MenuCamera GameObject</returns>
    private (Animator, GameObject) FindMenuObjects() 
    {
        if(!SceneManager.GetActiveScene().name.StartsWith("Menu"))
            return (null, null);

        // Attempt to find menu transition
        GameObject mco = GameObject.Find("MenuCanvas");
        Animator menuCanvasAnimator = mco.GetComponent<Animator>();

        GameObject menuCamera = GameObject.Find("MenuCamera");
        menuCamera.transform.rotation = rotation;
        MenuCameraDrift mcd = menuCamera.GetComponent<MenuCameraDrift>();
        mcd.SetRotationVector(rotVector);
        mcd.SetRotationVectorSelectTime(Random.Range(15, 25));

        if(menuCanvasAnimator == null) {
            Debug.LogError("Failed to find menu canvas animator");
        }
        if(menuCamera == null) {
            Debug.LogError("Failed to find menu camera");
        }

        return (menuCanvasAnimator, menuCamera);
    }

}
