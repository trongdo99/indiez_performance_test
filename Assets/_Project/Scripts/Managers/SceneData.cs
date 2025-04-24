using System;

[Serializable]
public class SceneData
{
    public SceneReference SceneAsset;
    public string ScenePath => SceneAsset.ScenePath;
    public string Name => SceneAsset.Scenename;
}