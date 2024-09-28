using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class SpawnPositions : MonoBehaviour
{

    [Range(0f, 2f)]
    [SerializeField] private float spawnPositionSize = 1f;
    public Vector3 spawnForward;
    private void OnDrawGizmos()
    {
        int i = 0;
        foreach(Transform t in transform)
        {
            switch(i) {
                case 0: Gizmos.color = Color.yellow; break;
                case 1: Gizmos.color = new Color(0.7f, 0.7f, 0.7f); break;
                case 2: Gizmos.color = new Color(186/255f, 123/255f, 82/255f); break;
                default: Gizmos.color = Color.cyan; break;
            }
            Gizmos.DrawWireSphere(t.position, spawnPositionSize);
            i++;
        }
    }

}