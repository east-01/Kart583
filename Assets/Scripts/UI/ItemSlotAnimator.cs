using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ItemSlotAnimator : MonoBehaviour
{

    /* Editor fields*/
    public GameObject itemImagePrefab;

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
            if(timeTillSpawn <= 0) {

                bool lastImage = animationTime + (singleImageDuration/2f) >= overallDuration;
                if(lastImage) 
                    SpawnNewImage(true, this.result);
                else
                    SpawnNewImage(false, GetRandomItem());
            }
        }

    }

    private void SpawnNewImage(bool stopAtCenter, Item itemToSpawn) 
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
            imageObj = Instantiate(itemImagePrefab, transform); //instantiate according to passed through item selection
            animatingItemImages.Add(imageObj);
        }

        imageObj.GetComponent<ItemImage>().StartAnimation(itemToSpawn, startPosition, centerPosition, endPosition, singleImageDuration, stopAtCenter);
    }

    /** Animate a single ItemImage once with parameters. */
    public void AnimateItems(Item result) 
    {
        if(IsAnimating()) return;
        this.animationTime = 0;
        this.animating = true;
        this.result = result;
    }

    public static Item GetRandomItem() 
    {
        Array values = Enum.GetValues(typeof(Item));
        int randomIndex = UnityEngine.Random.Range(0, values.Length);
        return (Item)values.GetValue(randomIndex);
    }

    public bool IsAnimating() { return animating; }
    public void DisableChildren() { animatingItemImages.ForEach(imageObj => imageObj.SetActive(false)); }

}
