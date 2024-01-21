using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour
{

    [SerializeField] GameObject transitionDelegatePrefab;

    /* Fade in/out related objects */
    public Animator transition;
    public GameObject child;
    public float transitionTime = 0.5f;
    
    private Animator menuTransition;
    private TransitionDelegate transitionDelegate;

    void Awake() 
    {
        child.SetActive(true);

        // Look for transition delegate
        GameObject td = GameObject.Find("TransitionDelegate");
        if(td == null) {
            td = Instantiate(transitionDelegatePrefab);
            td.name = "TransitionDelegate";
            DontDestroyOnLoad(td);   
        }
        transitionDelegate = td.GetComponent<TransitionDelegate>();

        // Attempt to find menu transition
        if(SceneManager.GetActiveScene().name.StartsWith("Menu")) {
            GameObject mco = GameObject.Find("MenuCanvas");
            menuTransition = mco.GetComponent<Animator>();

            GameObject menuCamera = GameObject.Find("MenuCamera");
            menuCamera.transform.rotation = transitionDelegate.rotation;
            MenuCameraDrift mcd = menuCamera.GetComponent<MenuCameraDrift>();
            mcd.SetRotationVector(transitionDelegate.rotVector);
            mcd.SetRotationVectorSelectTime(Random.Range(1, 3));
        }

        /* Wake up animation */
        if(!transitionDelegate.precedingScene.StartsWith("Menu") ||
           transitionDelegate.precedingScene == SceneNames.MENU_MAP) {
        }

    }

    public void LoadScene(string sceneName) 
    {
        StartCoroutine(LoadWithAnimation(sceneName));
    }

    IEnumerator LoadWithAnimation(string sceneName) 
    {
        bool isMenuTransition = SceneManager.GetActiveScene().name.StartsWith("Menu") && sceneName.StartsWith("Menu");
        if(isMenuTransition) {
            menuTransition.SetTrigger("Animate");
            yield return new WaitForSeconds(0.5f);
        } else {
            transition.SetTrigger("FadeToBlack");
            yield return new WaitForSeconds(transitionTime);
        }

        // Set transition delegate fields
        transitionDelegate.precedingScene = SceneManager.GetActiveScene().name;
        if(SceneManager.GetActiveScene().name.StartsWith("Menu")) {
            GameObject menuCamera = GameObject.Find("MenuCamera");
            transitionDelegate.rotation = menuCamera.transform.rotation;
            transitionDelegate.rotVector = menuCamera.GetComponent<MenuCameraDrift>().GetRotationVector();
        }

        SceneManager.LoadScene(sceneName);
    }
    
}
