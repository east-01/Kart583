using System;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine 
{
    private IState _currentState;

    private Dictionary<Type, List<Transition>> _transitions = new Dictionary<Type, List<Transition>>();
    private List<Transition> _currentTransitions = new List<Transition>();
    private List<Transition> _anyTransitions = new List<Transition>();

    private static List<Transition> EmptyTransitions = new List<Transition>(0);

    public void Tick()
    {
        var transition = GetTransition();

        if (transition != null)
        {
            SetState(transition.ToState);
        }

        _currentState?.OnTick();
    }

    public void SetState(IState state)
    {
        if (state == _currentState)
        {
            return;
        }

        _currentState?.OnStateExit();
        _currentState = state;

        _transitions.TryGetValue(_currentState.GetType(), out _currentTransitions);

        if (_currentTransitions == null)
        {
            _currentTransitions = EmptyTransitions;
        }

        _currentState.OnStateEnter();
    }

    public void AddTranstion(IState fromState, IState toState, Func<bool> predicate)
    {
        if (_transitions.TryGetValue(fromState.GetType(), out var transitions) == false)
        {
            transitions = new List<Transition>();
            _transitions[fromState.GetType()] = transitions;
        }

        transitions.Add(new Transition(toState, predicate));
    }

    public void AnyTransition(IState state, Func<bool> predicate)
    {
        _anyTransitions.Add(new Transition(state, predicate));
    }

    private class Transition
    {
        public Func<bool> Condition { get; }
        public IState ToState { get; }

        public Transition(IState toState, Func<bool> condition)
        {
            ToState = toState;
            Condition = condition;
        }
    }

    private Transition GetTransition()
    {
        foreach(var transition in _anyTransitions)
        {
            if (transition.Condition())
            {
                return transition;
            }
        }

        foreach (var transition in _currentTransitions)
        {
            if (transition.Condition())
            {
                return transition;
            }
        }

        return null;
    }
}

