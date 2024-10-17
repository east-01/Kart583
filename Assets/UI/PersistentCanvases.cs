using EMullen.Bootstrapper;
using UnityEngine;

public class PersistentCanvases : MonoBehaviour, IBootstrapComponent
{
    
    public static PersistentCanvases Instance { get; private set; }

    private void Awake() 
    {
        if(Instance != null) {
            Debug.LogError($"New PersistentCanvases object loaded when the singletone is already instantiated, deleting gameObject \"{gameObject.name}\"");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsLoadingComplete() => true;
}