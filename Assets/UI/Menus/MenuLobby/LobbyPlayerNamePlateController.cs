using EMullen.PlayerMgmt;
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
        if(!data.HasData<PlayerDisplayData>()) {
            Debug.LogError($"Can't ShowPlayerData for player uid \"{data.GetUID()}\" they don't have PlayerDisplayData.");
            return;
        }
        if(!data.HasData<RaceData>()) {
            Debug.LogError($"Can't ShowPlayerData for player uid \"{data.GetUID()}\" they don't have RaceData.");
            return;
        }
        PlayerDisplayData displayData = data.GetData<PlayerDisplayData>();
        RaceData raceData = data.GetData<RaceData>();
        playerNameText.text = displayData.name;
        playerScoreText.text = raceData.points + "";

        kartImage.sprite = CoreManager.KartAtlas.RetrieveData(raceData.kartType).image;
    }

}
