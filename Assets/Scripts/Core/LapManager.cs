using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LapManager : MonoBehaviour
{
    [SerializeField]
    private GameObject _lapStart;

    [SerializeField]
    private GameObject _midLap;

    private Dictionary<GameObject, bool> _hasPastMidpoint = new Dictionary<GameObject, bool>();
    private Dictionary<GameObject, bool> _hasPastStartpoint = new Dictionary<GameObject, bool>();

    public Dictionary<GameObject, int> KartLaps = new Dictionary<GameObject, int>();


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TriggerLap(GameObject gameObject, LapTrigger lapTrigger)
    {
        GameObject go = lapTrigger.gameObject;

        if (go == _midLap)
        {
            if (_hasPastMidpoint.ContainsKey(gameObject))
            {
                _hasPastMidpoint[gameObject] = true;
            }
            else
            {
                _hasPastMidpoint.Add(gameObject, true);
            }
        }
        if (go == _lapStart)
        {
            if (_hasPastStartpoint.ContainsKey(gameObject))
            {
                _hasPastStartpoint[gameObject] = true;
            }
            else
            {
                _hasPastStartpoint.Add(gameObject, true);
            }

            bool start = _hasPastStartpoint.TryGetValue(gameObject, out bool startValue);
            bool end = _hasPastMidpoint.TryGetValue(gameObject, out bool endValue);

            if (start && end)
            {
                if (KartLaps.ContainsKey(gameObject))
                {
                    KartLaps[gameObject] += 1;

                    if (KartLaps[gameObject] >= 3)
                    {
                        GameStateManager gsm = GameObject.FindObjectOfType<GameStateManager>();
                        gsm.LapsCompleted = 3;
                    }
                }
                else
                {
                    KartLaps.Add(gameObject, 1);
                }

                _hasPastMidpoint[gameObject] = false;
                _hasPastStartpoint[gameObject] = false;

                Debug.Log(gameObject.name + " has finished lap " + KartLaps[gameObject]);
            }

        }
    }
}
