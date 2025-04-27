using System;
using UnityEngine;

public class MainMenuUIManager : MonoBehaviour
{
    public async void PlayButtonClick()
    {
        try
        {
            // TODO: Load correct level
            await SceneLoader.Instance.LoadSceneAsync("Level1");
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }
}
