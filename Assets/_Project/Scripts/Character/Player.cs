using System;
using System.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

public class Player : MonoBehaviour, ISyncInitializable
{
    public event Action OnPlayerDeathAnimationCompleted;
    
    [SerializeField] private InputReader _input;
    [SerializeField] private GameObject _playerCharacterPrefab;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private CinemachineCamera _topDownCamera;
    
    private PlayerCharacterController _playerCharacterController;
    
    public Transform PlayerCharacterTransform => _playerCharacterController.transform;

    public void Initialize(IProgress<float> progress = null)
    {
        progress?.Report(0f);
        
        _input.EnablePlayerActions();
        SpawnPlayerCharacter(_spawnPoint.position, _spawnPoint.rotation);
    }

    private void Update()
    {
        if (GameplayManager.Instance.IsGamePaused) return;
        
        if (_playerCharacterController != null)
        {
            _playerCharacterController.SetMoveInput(_input.MoveInput);
            _playerCharacterController.SetLookInput(_input.LookInput);
        }
    }

    public void SpawnPlayerCharacter(Vector3 position, Quaternion rotation)
    {
        if (_playerCharacterController != null)
        {
            DestroyPlayerCharacter();
        }
        
        GameObject playerCharacterObj = Instantiate(_playerCharacterPrefab, position, rotation);
        _playerCharacterController = playerCharacterObj.GetComponent<PlayerCharacterController>();

        if (_playerCharacterController == null)
        {
            Debug.LogError("PlayerCharacterController isn't found on player prefab");
            Destroy(playerCharacterObj);
            return;
        }
        
        _playerCharacterController.OnDeath += HandlePlayerCharacterDeath;
        _playerCharacterController.OnDeathAnimationComplete += HandlePlayerAnimationDeathComplete;
    }

    public void SetupPlayerFollowCamera(CinemachineCamera topDownCamera)
    {
        _topDownCamera = topDownCamera;
        _topDownCamera.Follow = _playerCharacterController.transform;
    }

    public void DestroyPlayerCharacter()
    {
        if (_playerCharacterController == null) return;
        
        _playerCharacterController.OnDeath -= HandlePlayerCharacterDeath;
        _playerCharacterController.OnDeathAnimationComplete -= HandlePlayerAnimationDeathComplete;
        
        Destroy(_playerCharacterController.gameObject);
        _playerCharacterController = null;
    }

    private void RespawnPlayerCharacter()
    {
        SpawnPlayerCharacter(_spawnPoint.position, _spawnPoint.rotation);
    }

    private void HandlePlayerCharacterDeath()
    {
        // noop
    }

    private void HandlePlayerAnimationDeathComplete()
    {
        OnPlayerDeathAnimationCompleted?.Invoke();
    }
}
