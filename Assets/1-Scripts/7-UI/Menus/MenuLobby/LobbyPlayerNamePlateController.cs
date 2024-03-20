using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerNamePlateController : MonoBehaviour
{

    [SerializeField]
    private GameObject atlasesPrefab;

    [SerializeField]
    private TMP_Text playerNameText;
    [SerializeField]
    private TMP_Text playerScoreText;
    [SerializeField]
    private Image kartImage;

    public void ShowPlayerData(PlayerData data) 
    {
        playerNameText.text = data.name;
        playerScoreText.text = "0";

        KartAtlas ka = atlasesPrefab.GetComponent<KartAtlas>();
        kartImage.sprite = ka.RetrieveData(data.kartType).image;
    }

}
