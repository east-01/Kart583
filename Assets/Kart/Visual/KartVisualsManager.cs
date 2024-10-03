using System.Collections.Generic;
using Steamworks;
using TMPro;
using UnityEngine;

public class KartVisualsManager : KartBehavior
{
    [SerializeField]
    private GameObject bumpParticlePrefab;
    [SerializeField]
    private GameObject landParticlePrefab;

    [SerializeField]
    private GameObject nameplate;

    private bool isModelLoaded = false;
    private bool isNameplateLoaded = false;
    
    private void Update() 
    {
        PlayerData playerData = kartManager.PlayerData;
        PlayerObjectManager pom = PlayerManager.Instance;

        if(!isModelLoaded && 
           playerData.kartType != KartType.NONE)
            LoadKartModel();

        if(!isNameplateLoaded && 
           base.IsClient && kartManager.POIGDelegate == null && // Should load nameplate?
           pom != null && pom.PlayerCount > 0 && pom.PlayerObjects[0].poigDelegate != null && pom.PlayerObjects[0].poigDelegate.Camera != null)
            LoadNameplate();

        // Unload nameplate since this is the player's own kart
        if(isNameplateLoaded && kartManager.POIGDelegate != null)
            nameplate.SetActive(false);

    }

    public void LoadKartModel() 
    {
        KartDataPackage kdp = CoreManager.KartAtlas.RetrieveData(kartManager.PlayerData.kartType);
		kartCtrl.settings = kdp.settings;
	
        /* New kart model */
		GameObject newKartModelGameObject = Instantiate(kdp.model.gameObject, transform);
        KartModel newKartModel = newKartModelGameObject.GetComponent<KartModel>();
		newKartModel.SetKartController(kartCtrl);
		kartCtrl.kartModelTransform = newKartModelGameObject.transform;
        kartCtrl.kartModel = newKartModel;

        /* Hit item */
        Transform hit = newKartModel.heldItemTransform;
        kartItemManager.heldItemImage.transform.SetPositionAndRotation(hit.position, hit.rotation);

		// if(kartCtrl.kartModel != null) 
		// 	kartCtrl.initKartModelY = kartCtrl.kartModel.localPosition.y;
		// else
		// 	Debug.LogWarning("KartController on \"" + kartCtrl.gameObject.name + "\" doesn't have a kartModel assigned."); 

        isModelLoaded = true;
    }

    public void LoadNameplate() 
    {  
        nameplate.SetActive(true);

        TMP_Text npt = nameplate.GetComponentInChildren<TMP_Text>();
        Billboard npb = nameplate.GetComponentInChildren<Billboard>();

        npt.text = kartManager.PlayerData.name;
        npb.focusCamera = PlayerManager.Instance.PlayerObjects[0].poigDelegate.Camera;

        isNameplateLoaded = true;
    }

    public void SpawnBumpEffect(Vector3 position) 
    {
        GameObject particles = Instantiate(bumpParticlePrefab);
        particles.transform.position = position;
    }

    public void SpawnLandEffect(Transform position) 
    {
        List<Vector3> wheelPositions = kartCtrl.kartModel.WheelPositions;

        CoreManager.AudioManager.PlayOneShotSound(AudioFile.KART_LAND, 0.25f, position);

        kartCtrl.kartModel.WheelPositions.ForEach(wheelPos => {
            GameObject particles = Instantiate(landParticlePrefab);
            particles.transform.position = position.position;
        });
    }

}
