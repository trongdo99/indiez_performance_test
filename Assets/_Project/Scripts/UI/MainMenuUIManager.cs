using UnityEngine;

public class MainMenuUIManager : MonoBehaviour
{
    public async void PlayButtonClick()
    {
        await SceneLoader.Instance.LoadSceneAsync(1);
    }
}
