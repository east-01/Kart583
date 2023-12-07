using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/** This script goes on the ItemDisplay object in the player's HUD, responsible
  *   for showing item icons to the player. It uses the itemImagePrefab to do the
  *   actual showing of images. */
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

        // Check if we're done animating
        if(animationTime >= overallDuration) {
            animating = false;
            animationTime = 0;
            timeTillSpawn = 0;
            return;
        }

        // Decrement the time till spawn and spawn a new one if needed.
        timeTillSpawn -= Time.deltaTime;    
        if(timeTillSpawn <= 0) {

            // Check if (the animation time) + (the animation time it takes to get to center) >= animation duration
            bool lastImage = animationTime + (singleImageDuration/2f) >= overallDuration;
            if(lastImage) 
                SpawnNewImage(true, this.result);
            else
                SpawnNewImage(false, GetRandomItem());
        }

    }

    /** Start a new item animation. The animation is comprised of single item images animating
      *   in quick succession to look like a spinning wheel. Here, the result is the item
      *   awarded to the player when the animation completes. */
    public void AnimateItems(Item result) 
    {
        if(IsAnimating()) return;
        this.animationTime = 0;
        this.animating = true;
        this.result = result;
    }

    /** This spawns a single item image to animate, further explained by ItemImage#StartAnimation().
      * This method is in place to figure out which item image prefab to use from the already
      *   spawned item image prefabs (as a memory saving measure) then activating it with parameters 
      *   in StartAnimation(). */
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

    /** Pick an item enum completely randomly from all options. */
    public static Item GetRandomItem() 
    {
        Array values = Enum.GetValues(typeof(Item));
        int randomIndex = UnityEngine.Random.Range(0, values.Length);
        return (Item)values.GetValue(randomIndex);
    }

    public bool IsAnimating() { return animating; }
    public void DisableChildren() { animatingItemImages.ForEach(imageObj => imageObj.SetActive(false)); }

}
