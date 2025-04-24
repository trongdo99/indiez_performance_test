using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif


// Based on
// https://github.com/JohannesMP/unity-scene-reference?tab=readme-ov-file

[Serializable]
public class SceneReference : ISerializationCallbackReceiver
{
#if UNITY_EDITOR
    [SerializeField] private Object _sceneAsset;
    private bool IsValidSceneAsset
    {
        get
        {
            if (!_sceneAsset) return false;

            return _sceneAsset is UnityEditor.SceneAsset;
        }
    }
#endif

    [SerializeField]
    private string _scenePath = string.Empty;

    public string ScenePath
    {
        get
        {
#if UNITY_EDITOR
            return GetScenePathFromAsset();
#else
            return _scenePath;
#endif
        }
        set
        {
            _scenePath = value;
#if UNITY_EDITOR
            _sceneAsset = GetSceneAssetFromPath();
#endif
        }
    }

    public string Scenename
    {
        get
        {
            string path = ScenePath;
            if (string.IsNullOrEmpty(path)) return string.Empty;
            
            int lastSlash = path.LastIndexOf('/');
            string fileName = lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
            int lastDot = fileName.LastIndexOf('.');
            return lastDot >= 0 ? fileName.Substring(0, lastDot) : fileName;
        }
    }

    public static implicit operator string(SceneReference sceneReference)
    {
        return sceneReference.ScenePath;
    }

    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR
        HandleBeforeSerialize();
#endif
    }

    public void OnAfterDeserialize()
    {
#if UNITY_EDITOR
        EditorApplication.update += HandleAfterDeserialize;
#endif
    }



#if UNITY_EDITOR
    private UnityEditor.SceneAsset GetSceneAssetFromPath()
    {
        return string.IsNullOrEmpty(_scenePath) ? null : AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(_scenePath);
    }

    private string GetScenePathFromAsset()
    {
        return _sceneAsset == null ? string.Empty : AssetDatabase.GetAssetPath(_sceneAsset);
    }

    private void HandleBeforeSerialize()
    {
        if (IsValidSceneAsset == false && string.IsNullOrEmpty(_scenePath) == false)
        {
            _sceneAsset = GetSceneAssetFromPath();
            if (_sceneAsset == null) _scenePath = string.Empty;

            EditorSceneManager.MarkAllScenesDirty();
        }
        else
        {
            _scenePath = GetScenePathFromAsset();
        }
    }

    private void HandleAfterDeserialize()
    {
        EditorApplication.update -= HandleAfterDeserialize;
        if (IsValidSceneAsset) return;

        if (string.IsNullOrEmpty(_scenePath)) return;

        _sceneAsset = GetSceneAssetFromPath();
        if (!_sceneAsset) _scenePath = string.Empty;

        if (!Application.isPlaying) EditorSceneManager.MarkAllScenesDirty();
    }
#endif
}
