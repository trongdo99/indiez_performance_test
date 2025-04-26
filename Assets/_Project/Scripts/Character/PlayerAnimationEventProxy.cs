using System;
using UnityEngine;

public class PlayerAnimationEventProxy : MonoBehaviour
{
    public event Action AnimationDieCompletedEvent;
    public event Action AnimationThrowEvent;
    public event Action AnimationThrowEndedEvent;
    
    public void AnimationDieCompleted()
    {
        AnimationDieCompletedEvent?.Invoke();
    }

    public void Throw()
    {
        AnimationThrowEvent?.Invoke();
    }

    public void AnimationThrowEnded()
    {
        AnimationThrowEndedEvent?.Invoke();
    }
}
