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
    private bool IsIntroFinished;

    private List<Camera> _playerCams = new List<Camera>();
    private List<GameObject> _karts = new List<GameObject>();

    private bool _isCameraMoveComplete;
    private bool _isKartsSpawned;

    private float _titleScreenTimer = 500f;

    private KartSpawnData _spawnPoints;
    private GameStateManager _gsm;

    public LevelEnter_GSM(GameStateManager gsm)
    {
        _gsm = gsm;
        _cam = gsm.Cam;
        _players = gsm.PlayerKarts;
        _bots = gsm.BotsPrefabs;
        _spawnPoints = GameObject.FindObjectOfType<KartSpawnData>();
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

        var camPos = _cam.GetComponent<IntroCamData>().CamStartPos;
        _cam.transform.position = camPos.transform.position;

        SpawnKarts();

        
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
        if (_titleScreenTimer <= 0)
        {
            CameraMove();
        }
        else
        {
            _titleScreenTimer--;
        }
    }

    private void CameraMove()
    {
        _cam.transform.position = Vector3.MoveTowards(_cam.transform.position, _cam.GetComponent<IntroCamData>().CamEndPos.position, 0.5f);

        if (_cam.transform.position == _cam.GetComponent<IntroCamData>().CamEndPos.position)
        {
            _gsm.IsIntroFinished = true;
        }
    }

    private void SpawnKarts()
    {
        for (int i = 0; i < _spawnPoints.SpawnPoints.Length; i++)
        {
            var go = GameObject.Instantiate(_karts[i]);
            go.SetActive(true);
            Transform modelTransform = go.transform.Find("Player");
            if (modelTransform)
            {
                modelTransform?.gameObject.SetActive(true);
                modelTransform.SetPositionAndRotation(_spawnPoints.SpawnPoints[i].position, new Quaternion(0, 180, 0, 0));
                Camera c = go.GetComponentInChildren<Camera>(true);
                c.enabled = false;
                _playerCams.Add(c);
            }
            else
            {
                go.transform.SetPositionAndRotation(_spawnPoints.SpawnPoints[i].position, new Quaternion(0, 180, 0, 0));
            }
        }
    }
}
