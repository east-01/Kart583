using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableLevelName : MonoBehaviour
{
    private float ttl;
    private float timer;

    private void Awake()
    {
        ttl = 6f;
        timer = ttl;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (timer <= 0)
        {
            Destroy(gameObject);
        }
        else
        {
            timer -= Time.deltaTime;
        }
    }
}
