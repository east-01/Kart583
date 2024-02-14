using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Acts as a mediator between the KartManager and all the PlayerUI elements.
/// This solution is better than attaching each hud element to the KartManager 
///   individually in the editor, where we only have to attach the KM once here.
/// </summary>
public class PlayerHUDCanvas : MonoBehaviour
{
    public KartManager subject;
}
