using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    private OnScreenStick _onScreenStick;

    private void Awake()
    {
        _onScreenStick = GetComponentInChildren<OnScreenStick>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _onScreenStick.OnPointerDown(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _onScreenStick.OnPointerUp(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _onScreenStick.OnDrag(eventData);
    }
}
