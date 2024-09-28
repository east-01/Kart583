using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerNamePlateController : MonoBehaviour
{

    [SerializeField]
    private TMP_Text playerNameText;
    [SerializeField]
    private TMP_Text playerScoreText;
    [SerializeField]
    private Image kartImage;

    public void ShowPlayerData(PlayerData data) 
    {
        playerNameText.text = data.name;
        playerScoreText.text = data.points + "";

        kartImage.sprite = CoreManager.KartAtlas.RetrieveData(data.kartType).image;
    }

}
