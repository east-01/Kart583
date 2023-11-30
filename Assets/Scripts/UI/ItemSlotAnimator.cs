using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSlotAnimator : MonoBehaviour
{

    /* Editor fields*/
    public GameObject oilImagePrefab;
    public GameObject boltImagePrefab;
    public List<GameObject> itemImagePrefabs;

    [Header("Animation Settings")]
    public float overallDuration;
    public float singleImageDuration;
    public float singleImageFrequency;
    public RectTransform startPosition;
    public RectTransform centerPosition;
    public RectTransform endPosition;

    /* Fields set by AnimateItems() */
    private bool animating;
    private Item result;

    /* Other fields */
    private List<GameObject> animatingItemImages;
    [SerializeField] private float animationTime;
    [SerializeField] private float timeTillSpawn;
    void Start() 
    {
        itemImagePrefabs.Add(oilImagePrefab);
        itemImagePrefabs.Add(boltImagePrefab);

        if(animatingItemImages != null && animatingItemImages.Count > 0) 
            animatingItemImages.ForEach(itemImage => GameObject.Destroy(itemImage));
        animatingItemImages = new List<GameObject>();
    }

    void Update() {
        if(!animating) return;

        animationTime += Time.deltaTime;

        if(animationTime >= overallDuration) {
            animating = false;
            animationTime = 0;
            timeTillSpawn = 0;
            return;
        }

        if(timeTillSpawn > 0) {
            timeTillSpawn -= Time.deltaTime;
            if(timeTillSpawn <= 0) 
                SpawnNewImage(animationTime + (singleImageDuration/2f) >= overallDuration, Item.OIL);
        }

    }

    private void SpawnNewImage(bool stopAtCenter, Item result) 
    {
        this.timeTillSpawn = singleImageFrequency;

        GameObject imageObj = null;
        // Search for a disabled (already spawned) image
        foreach(GameObject existingImgObj in animatingItemImages) {
            if(!existingImgObj.activeSelf) {
                imageObj = existingImgObj;
                break;
            }
        }

        // Didn't find an existing object, spawn new one
        if(imageObj == null) {
            imageObj = GameObject.Instantiate(itemImagePrefabs.ToArray()[(int)result], transform); //instantiate according to passed through item selection
            animatingItemImages.Add(imageObj);
        }

        imageObj.GetComponent<ItemImage>().StartAnimation(startPosition, centerPosition, endPosition, singleImageDuration, stopAtCenter);
    }

    /** Animate a single ItemImage once with parameters. */
    public void AnimateItems(Item result) 
    {
        if(IsAnimating()) return;
        this.animationTime = 0;
        this.animating = true;
        this.result = result;
        SpawnNewImage(false, result);
    }

    public bool IsAnimating() { return animating; }
    public void DisableChildren() { animatingItemImages.ForEach(imageObj => imageObj.SetActive(false)); }

}
