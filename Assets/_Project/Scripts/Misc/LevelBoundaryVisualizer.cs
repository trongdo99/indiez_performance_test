using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class LevelBoundaryVisualizer : MonoBehaviour
{
    [SerializeField] private Color _boundaryColor = new Color(0f, 1f, 0f, 0.2f);
    [SerializeField] private float _lineWidth = 0.1f;
    
    private BoxCollider _boxCollider;

    private void Start()
    {
        _boxCollider = GetComponent<BoxCollider>();
    }
    
    private void OnDrawGizmos()
    {
        if (_boxCollider == null)
        {
            _boxCollider = GetComponent<BoxCollider>();
        }
        
        Gizmos.color = _boundaryColor;
        Matrix4x4 originalMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(_boxCollider.center, _boxCollider.size);
        
        DrawBoxEdges(_boxCollider.center, _boxCollider.size);
        
        Gizmos.matrix = originalMatrix;
    }

    private void DrawBoxEdges(Vector3 center, Vector3 size)
    {
        Vector3 halfSize = size * 0.5f;
        
        var corners = new Vector3[8];
        corners[0] = center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
        corners[1] = center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
        corners[2] = center + new Vector3(halfSize.x, -halfSize.y, halfSize.z);
        corners[3] = center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
        corners[4] = center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
        corners[5] = center + new Vector3(halfSize.x, halfSize.y, -halfSize.z);
        corners[6] = center + new Vector3(halfSize.x, halfSize.y, halfSize.z);
        corners[7] = center + new Vector3(-halfSize.x, halfSize.y, halfSize.z);
        
        // Draw the 12 edges
        // Bottom face
        DrawThickLine(corners[0], corners[1]);
        DrawThickLine(corners[1], corners[2]);
        DrawThickLine(corners[2], corners[3]);
        DrawThickLine(corners[3], corners[0]);
        
        // Top face
        DrawThickLine(corners[4], corners[5]);
        DrawThickLine(corners[5], corners[6]);
        DrawThickLine(corners[6], corners[7]);
        DrawThickLine(corners[7], corners[4]);
        
        // Connecting edges
        DrawThickLine(corners[0], corners[4]);
        DrawThickLine(corners[1], corners[5]);
        DrawThickLine(corners[2], corners[6]);
        DrawThickLine(corners[3], corners[7]);
    }
    
    private void DrawThickLine(Vector3 start, Vector3 end)
    {
        Vector3 dir = (end - start).normalized;
        Vector3 cross = Vector3.Cross(dir, Camera.current != null ? Camera.current.transform.forward : Vector3.forward).normalized;
        
        // Adjust line width based on distance to camera (optional)
        float width = _lineWidth;
        if (Camera.current != null)
        {
            float distance = Vector3.Distance(Camera.current.transform.position, (start + end) * 0.5f);
            width *= Mathf.Max(1f, distance * 0.1f);
        }
        
        Vector3 offset1 = cross * width;
        Vector3 offset2 = -cross * width;
        
        // Draw multiple lines for thickness
        Gizmos.DrawLine(start + offset1, end + offset1);
        Gizmos.DrawLine(start + offset2, end + offset2);
        Gizmos.DrawLine(start, end);
    }
}
