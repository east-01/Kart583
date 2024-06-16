using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

/// <summary>
/// A BlockingVariable is a type that other objects must wait to to use. Subscribe to the assignment
///   events if you want use the value for initialization.
///
/// Example usage: You have a GameMap class that you have to wait for initialization to complete
///   to use, so you make GameMap a BlockingVariable and use a guard statement to check if the
///   BlockingVariable is valid in your Update method.
/// </summary>
public class BlockingVariable<T> where T : IBlockingObject
{
    public bool IsValid => (Value != null) && Value.IsValid;
    public T Value {
        get { return blockingVariable; }
        set {
            if(this.blockingVariable == null && initialAssign == false) {
                initialAssign = true;
                blockingVariable = value;
                BlockingVariableInitialAssignEvent?.Invoke(value);
            } else {
                T prev = blockingVariable;
                blockingVariable = value;
                BlockingVariableReassignedEvent?.Invoke(prev, value);
            }
        }
    }

    private T blockingVariable;
    private bool initialAssign = false;

    /// <summary>
    /// Event for when the blocking variable is initially assigned. Called after assignment.
    /// </summary>
    public delegate void BlockingVariableInitialAssignHandler(T value);
    /// <summary> See delegate BlockingVariable#BlockingVariableInitialAssignHandler </summary>
    public event BlockingVariableInitialAssignHandler BlockingVariableInitialAssignEvent;
    /// <summary>
    /// Event for when the block variable is re-assigned, or when the blocking variable is assigned
    ///   when an existing blocking variable already exists. Called after variable is re-assigned,
    ///   previous state is supplied as prevValue (prevValue can be null if not the initial assignment).
    /// <summary>
    public delegate void BlockingVariableReassignedHandler(T prevValue, T currentValue);
    /// <summary> See delegate BlockingVariable#BlockingVariableInitialAssignHandler </summary>    
    public event BlockingVariableReassignedHandler BlockingVariableReassignedEvent;
}
