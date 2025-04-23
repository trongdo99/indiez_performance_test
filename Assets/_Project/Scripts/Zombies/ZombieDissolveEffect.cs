using System;
using System.Collections;
using UnityEngine;

public class ZombieDissolveEffect : MonoBehaviour
{
    public event Action OnDissolveCompleted;
    
    [SerializeField] private SkinnedMeshRenderer _skinnedMeshRenderer;
    [SerializeField] private float _dissolvedSpeed = 1f;
    [SerializeField] private bool _turnOffShadow = true;
    
    private MaterialPropertyBlock _propertyBlock;
    

    private void Start()
    {
        _propertyBlock = new MaterialPropertyBlock();
    }

    public void StartDissolveEffect()
    {
        if (_turnOffShadow)
        {
            _skinnedMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        
        StartCoroutine(DissolveCoroutine());
    }

    private IEnumerator DissolveCoroutine()
    {
        var dissolveAmount = 0f;
        while (dissolveAmount < 1f)
        {
            dissolveAmount += _dissolvedSpeed * Time.deltaTime;
            _propertyBlock.SetFloat("_DissolveAmount", dissolveAmount);
            _skinnedMeshRenderer.SetPropertyBlock(_propertyBlock);
            yield return null;
        }
        
        _propertyBlock.SetFloat("_DissolveAmount", 1f);
        _skinnedMeshRenderer.SetPropertyBlock(_propertyBlock);
        OnDissolveCompleted?.Invoke();
    }
}
