using UnityEngine;

[RequireComponent(typeof(TargetFinder))]
public class TargetFinderDebugVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [SerializeField] private bool _showDebugVisuals = true;
    [SerializeField] private bool _showInEditor = true;
    [SerializeField] private bool _showInGame = true;
    
    [Header("Cone Visualization")]
    [SerializeField] private Color _coneColor = Color.red;
    [SerializeField] private int _coneSegments = 50;
    [SerializeField] private float _coneAlpha = 0.3f;
    
    [Header("Target Visualization")]
    [SerializeField] private Color _targetLineColor = Color.green;
    [SerializeField] private bool _showTargetMarker = true;
    [SerializeField] private float _targetMarkerSize = 0.5f;
    
    [Header("Detection Range")]
    [SerializeField] private bool _showDetectionRange = true;
    [SerializeField] private Color _detectionRangeColor = new Color(0.2f, 0.2f, 1f, 0.1f);
    
    private TargetFinder _targetFinder;
    private PlayerCharacterController _playerController;
    
    private void Awake()
    {
        _targetFinder = GetComponent<TargetFinder>();
        _playerController = GetComponent<PlayerCharacterController>();
    }
    
    private void LateUpdate()
    {
        if (!ShouldDraw()) return;
        
        DrawDetectionRange();
        DrawAimCone();
        DrawTargetLine();
    }
    
    private bool ShouldDraw()
    {
        // Don't draw if debug visuals are disabled
        if (!_showDebugVisuals) return false;
        
        // Don't draw in editor if not enabled
        if (!Application.isPlaying && !_showInEditor) return false;
        
        // Don't draw in game if not enabled 
        if (Application.isPlaying && !_showInGame) return false;
        
        return true;
    }
    
    private void DrawDetectionRange()
    {
        if (!_showDetectionRange) return;
        
        // Draw detection sphere
        Debug.DrawLine(
            transform.position, 
            transform.position + Vector3.forward * _targetFinder.DetectionRadius, 
            _detectionRangeColor
        );
        
        Debug.DrawLine(
            transform.position, 
            transform.position + Vector3.back * _targetFinder.DetectionRadius, 
            _detectionRangeColor
        );
        
        Debug.DrawLine(
            transform.position, 
            transform.position + Vector3.left * _targetFinder.DetectionRadius, 
            _detectionRangeColor
        );
        
        Debug.DrawLine(
            transform.position, 
            transform.position + Vector3.right * _targetFinder.DetectionRadius, 
            _detectionRangeColor
        );
    }
    
    private void DrawAimCone()
    {
        // Only draw cone if we have input
        if (_playerController == null || _playerController.LookInput.sqrMagnitude < 0.01f) return;
        
        Vector3 aimDirection = new Vector3(
            _playerController.LookInput.x, 
            0, 
            _playerController.LookInput.y
        ).normalized;
        
        if (aimDirection.sqrMagnitude < 0.01f) return;
        
        DrawCone(aimDirection);
    }
    
    private void DrawCone(Vector3 aimDirection)
    {
        Color coneColor = new Color(
            _coneColor.r, 
            _coneColor.g, 
            _coneColor.b, 
            _coneAlpha
        );
        
        Vector3 origin = transform.position;
        float coneAngle = 0f;
        float coneRadius = _targetFinder.DetectionRadius;
        
        coneAngle = _targetFinder.ConeAngle;
        
        // Draw central aim ray
        Debug.DrawRay(origin, aimDirection * coneRadius, Color.yellow);
        
        // Draw cone outline
        for (int i = 0; i <= _coneSegments; i++)
        {
            float t = i / (float)_coneSegments;
            float currentAngle = Mathf.Lerp(-coneAngle / 2, coneAngle / 2, t);
            Quaternion rot = Quaternion.AngleAxis(currentAngle, Vector3.up);
            Vector3 dir = rot * aimDirection;
            
            Debug.DrawRay(origin, dir * coneRadius, coneColor);
            
            // Connect adjacent rays to form the arc
            if (i > 0)
            {
                float prevAngle = Mathf.Lerp(-coneAngle / 2, coneAngle / 2, (i-1) / (float)_coneSegments);
                Quaternion prevRot = Quaternion.AngleAxis(prevAngle, Vector3.up);
                Vector3 prevDir = prevRot * aimDirection;
                
                // Draw arc segments at distance
                Debug.DrawLine(
                    origin + prevDir * coneRadius,
                    origin + dir * coneRadius,
                    coneColor
                );
            }
        }
    }
    
    private void DrawTargetLine()
    {
        if (!_targetFinder.HasTarget) return;
        
        // Draw line to target
        Debug.DrawLine(
            transform.position, 
            _targetFinder.CurrentTarget.position, 
            _targetLineColor
        );
        
        // Draw target marker
        if (_showTargetMarker)
        {
            Vector3 targetPos = _targetFinder.CurrentTarget.position;
            float size = _targetMarkerSize;
            
            // Draw crosshair
            Debug.DrawLine(
                targetPos + Vector3.up * size, 
                targetPos + Vector3.down * size, 
                _targetLineColor
            );
            
            Debug.DrawLine(
                targetPos + Vector3.left * size, 
                targetPos + Vector3.right * size, 
                _targetLineColor
            );
            
            Debug.DrawLine(
                targetPos + Vector3.forward * size, 
                targetPos + Vector3.back * size, 
                _targetLineColor
            );
            
            // Draw small sphere
            float halfSize = size * 0.5f;
            DrawDebugCircle(targetPos, Vector3.up, halfSize, _targetLineColor);
            DrawDebugCircle(targetPos, Vector3.right, halfSize, _targetLineColor);
            DrawDebugCircle(targetPos, Vector3.forward, halfSize, _targetLineColor);
        }
    }
    
    private void DrawDebugCircle(Vector3 center, Vector3 normal, float radius, Color color, int segments = 16)
    {
        Vector3 tangent = Vector3.Cross(normal, normal == Vector3.up ? Vector3.right : Vector3.up).normalized;
        Vector3 biTangent = Vector3.Cross(normal, tangent);
        
        Vector3 previousPoint = center + tangent * radius;
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2;
            Vector3 currentPoint = center + 
                                  (tangent * Mathf.Cos(angle) + biTangent * Mathf.Sin(angle)) * radius;
            
            Debug.DrawLine(previousPoint, currentPoint, color);
            previousPoint = currentPoint;
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!_showInEditor || !_showDebugVisuals) return;
        
        if (_targetFinder == null)
        {
            _targetFinder = GetComponent<TargetFinder>();
            if (_targetFinder == null) return;
        }
        
        // Draw detection range sphere
        if (_showDetectionRange)
        {
            Gizmos.color = _detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, _targetFinder.DetectionRadius);
        }
    }
}