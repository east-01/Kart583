using System.Collections;
using System.Collections.Generic;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.UI;

/** This class is responsible for showing an animating a single item in an
  *   item slot animation.
  * We also have the option to stop the animation at the center, so the player
  *   can see what item they currently have. */
public class ItemImage : MonoBehaviour
{

    public AnimationCurve fadeCurve;
    public AnimationCurve sizeCurve;

    private RectTransform startPosition;
    private RectTransform centerPosition;
    private RectTransform endPosition;

    private float animationDuration;
    private float animationTime;
    private bool stopAtCenter;

    public Item item; // The item that is represented by this image, set by StartAnimation()

    void Update()
    {
        animationTime += Time.deltaTime;

        float progress = animationTime/animationDuration;
        if(progress >= 0.5f && stopAtCenter) return;
        if(progress >= 1f) {
            gameObject.SetActive(false);
        }
        
        // Get leg progress (first half, last half)
        // We do 1-legProgress in the last half so that we read the scale/fade curves backwards to mirror first half
        float legProgress = progress < 0.5f ? progress / 0.5f : 1-((progress-0.5f)/0.5f);

        // Change attributes
        // Start and end positions are different based on which leg we're in
        if(progress < 0.5f) {
            transform.position = Vector3.Lerp(startPosition.position, centerPosition.position, legProgress);
        } else {
            transform.position = Vector3.Lerp(centerPosition.position, endPosition.position, 1-legProgress); // Use 1-legProgress here to revert previous steps inversion
        }
                
        float size = sizeCurve.Evaluate(legProgress);
        transform.localScale = new Vector3(size, size, 0);

        Color newCol = GetComponent<SpriteRenderer>().color;
        newCol.a = fadeCurve.Evaluate(legProgress);
        GetComponent<SpriteRenderer>().color = newCol;
    }

    /** Activates the image game object and puts it in position for the animation.
        Use stopAtCenter = true to make the item stop at the center position. */
    public void StartAnimation(Item item, RectTransform startPosition, RectTransform centerPosition, RectTransform endPosition, float duration, bool stopAtCenter) 
    {
        this.item = item;
        this.startPosition = startPosition;
        this.centerPosition = centerPosition;
        this.endPosition = endPosition;
        this.animationDuration = duration;
        this.stopAtCenter = stopAtCenter;

        this.animationTime = 0;

        GameplayManager gameplay = FindObjectOfType<GameplayManager>();
        Debug.Log(gameplay.name);
        GetComponent<SpriteRenderer>().sprite = gameplay.GetComponent<ItemAtlas>().RetrieveData(item).itemIcon;

        gameObject.SetActive(true);

        transform.position = startPosition.position;
    }

}
