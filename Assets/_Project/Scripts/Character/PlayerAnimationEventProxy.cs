using System;
using UnityEngine;

public class PlayerAnimationEventProxy : MonoBehaviour
{
    public event Action AnimationDieCompletedEvent;
    
    public void AnimationDieCompleted()
    {
        AnimationDieCompletedEvent?.Invoke();
    }
}
