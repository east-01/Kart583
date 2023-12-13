using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelEnter_GSM : IState
{
    /*
     * This state will contain a quick camera traversal showing the level while the karts spawn.
     */
    private GameObject[] _players;
    private GameObject[] _bots;
    private Camera _cam;

    private List<Camera> _playerCams = new List<Camera>();
    private List<GameObject> _karts = new List<GameObject>();

    private GameStateManager _gsm;

    public LevelEnter_GSM(GameStateManager gsm)
    {
        _gsm = gsm;
        _cam = gsm.Cam;
        _players = gsm.PlayerKarts;
        _bots = gsm.BotsPrefabs;
    }

    public void OnStateEnter()
    {
        foreach (var player in _players)
        {
            _karts.Add(player);
            Camera c = player.GetComponentInChildren<Camera>(true);
            _playerCams.Add(c);

        }

        foreach (var bot in _bots)
        {
            _karts.Add(bot);
        }

        _cam.transform.position = GameplayManager.IntroCamData.CamStartPos.position;
        
    }

    public void OnStateExit()
    {
        Debug.Log("Player cams count: " + _playerCams.Count);
        foreach (var camera in _playerCams)
        {
            camera.gameObject.SetActive(true);
            camera.enabled = true;
        }
        
        Debug.Log("Leaving state: " + this.ToString());
    }

    public void OnTick()
    {

    }

}
