using UnityEditor.Rendering;
using UnityEngine;

public class AudioManagerMaster : AudioManager {

    public static AudioManagerMaster Instance { get; private set; }

    protected new void Awake() 
    {
        if(Instance != null) {
            Debug.LogError($"Another AudioManagerMaster instance exists, deleting gameObject \"{gameObject.name}\"");
            Destroy(gameObject);
            return;
        }

        base.Awake();

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

}