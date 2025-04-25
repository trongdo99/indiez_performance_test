using System;
using System.Collections.Generic;
using UnityEngine;

public interface IState
{
    void Enter();
    void Update();
    void LateUpdate();
    void Exit();
    void HandleCollision(Collision collision);
}

public abstract class StateMachine<TStateType> : MonoBehaviour where TStateType : Enum
{
    protected Dictionary<TStateType, IState> _states = new Dictionary<TStateType, IState>();
    protected IState _currentState;
    protected TStateType _currentStateType;
    protected TStateType _previousStateType;

    public TStateType CurrentStateType => _currentStateType;
    public TStateType PreviousStateType => _previousStateType;

    public virtual void ChangeState(TStateType newStateType)
    {
        if (_currentState != null)
        {
            _currentState.Exit();
        }

        _previousStateType = _currentStateType;
        _currentStateType = newStateType;
        _currentState = _states[newStateType];
        _currentState.Enter();
    }

    protected virtual void Update()
    {
        if (GameplayManager.Instance.IsGamePaused) return;
        _currentState?.Update();
    }

    protected virtual void LateUpdate()
    {
        if (GameplayManager.Instance.IsGamePaused) return;
        _currentState?.LateUpdate();
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        _currentState?.HandleCollision(collision);
    }
}
