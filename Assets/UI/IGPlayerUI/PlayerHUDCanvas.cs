using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Acts as a mediator between the KartManager and all the PlayerUI elements.
/// This solution is better than attaching each hud element to the KartManager 
///   individually in the editor, where we only have to attach the KM once here.
/// </summary>
public class PlayerHUDCanvas : MonoBehaviour
{
    public KartManager subject;

    public CountdownDisplay CountdownDisplay { get; private set; }
    public BoostDisplay BoostDisplay { get; private set; }
    public ItemSlotAnimator ItemDisplay { get; private set; }
    public PositionDisplay PositionDisplay { get; private set; }

    private void Awake() 
    {
        CountdownDisplay = GetComponentInChildren<CountdownDisplay>();
        BoostDisplay = GetComponentInChildren<BoostDisplay>();
        ItemDisplay = GetComponentInChildren<ItemSlotAnimator>();
        PositionDisplay = GetComponentInChildren<PositionDisplay>();

        List<string> problems = new();
        if(CountdownDisplay == null)
            problems.Add("PlayerHUDCanvas failed to connect to CountdownDisplay (should be a child of the PlayerHUDCanvas gameObject)");
        if(BoostDisplay == null)
            problems.Add("PlayerHUDCanvas failed to connect to BoostDisplay (should be a child of the PlayerHUDCanvas gameObject)");
        if(ItemDisplay == null)
            problems.Add("PlayerHUDCanvas failed to connect to ItemSlotAnimator (should be a child of the PlayerHUDCanvas gameObject)");
        if(PositionDisplay == null)
            problems.Add("PlayerHUDCanvas failed to connect to PositionDisplay (should be a child of the PlayerHUDCanvas gameObject)");
        
        if(problems.Count > 0) {
            Debug.LogError($"PlayerHUDCanvas experienced {problems.Count} problem(s)");
            problems.ForEach(p => Debug.LogError(" - " + p));
            gameObject.SetActive(false);
        }
    }

}
