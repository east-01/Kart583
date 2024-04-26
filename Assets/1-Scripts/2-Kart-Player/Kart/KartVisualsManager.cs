using Steamworks;
using TMPro;
using UnityEngine;

public class KartVisualsManager : KartBehavior
{
    [SerializeField]
    private GameObject bumpParticlePrefab;

    [SerializeField]
    private GameObject nameplate;

    private bool isModelLoaded = false;
    private bool isNameplateLoaded = false;

    private void Update() 
    {
        PlayerData playerData = kartManager.GetPlayerData();
        PlayerObjectManager pom = PlayerObjectManager.Instance;

        if(!isModelLoaded && 
           playerData.kartType != KartType.NONE)
            LoadKartModel();

        if(!isNameplateLoaded && 
           base.IsClient && kartManager.POIGDelegate == null && // Should load nameplate?
           pom != null && pom.PlayerObjectCount > 0 && pom.GetPlayerObjects()[0].poigDelegate != null && pom.GetPlayerObjects()[0].poigDelegate.Camera != null)
            LoadNameplate();

        // Unload nameplate since this is the player's own kart
        if(isNameplateLoaded && kartManager.POIGDelegate != null)
            nameplate.SetActive(false);

    }

    public void LoadKartModel() 
    {
        KartDataPackage kdp = CoreManager.KartAtlas.RetrieveData(kartManager.GetPlayerData().kartType);
		kartCtrl.settings = kdp.settings;
	
		GameObject newKartModel = Instantiate(kdp.model.gameObject, transform);
		newKartModel.GetComponent<KartModel>().SetKartController(kartCtrl);
		kartCtrl.kartModel = newKartModel.transform;

		if(kartCtrl.kartModel != null) 
			kartCtrl.initKartModelY = kartCtrl.kartModel.localPosition.y;
		else
			Debug.LogWarning("KartController on \"" + kartCtrl.gameObject.name + "\" doesn't have a kartModel assigned."); 

        isModelLoaded = true;
    }

    public void LoadNameplate() 
    {  
        nameplate.SetActive(true);

        TMP_Text npt = nameplate.GetComponentInChildren<TMP_Text>();
        Billboard npb = nameplate.GetComponentInChildren<Billboard>();

        npt.text = kartManager.GetPlayerData().name;
        npb.focusCamera = PlayerObjectManager.Instance.GetPlayerObjects()[0].poigDelegate.Camera;

        isNameplateLoaded = true;
    }

    public void SpawnBumpEffect(Vector3 position) 
    {
        GameObject particles = Instantiate(bumpParticlePrefab);
        particles.transform.position = position;
    }

}
