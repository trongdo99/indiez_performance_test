public static class GameEvents
{
    // Player events
    public struct PlayerDeathAnimationCompleted { }
    public struct PlayerHealthChanged { }
    
    // Waves events
    public struct AllWavesCompleted { }
    public struct WaveCompleted { }
    
    // Game state events
    public struct GameStateChanged { }
    
    // Game countdown events
    public struct GameStartingCountDown { }
    public struct GameStartingCountDownCompleted { }
}

public static class EventData
{
    public class WaveCompletedData
    {
        public int WaveNumber { get; set; }
    }
    
    public class GameStateChangedData
    {
        public GameplayStateType NewState { get; set; }
        public GameplayStateType PreviousState { get; set; }
    }

    public class GameStartingCountDownData
    {
        public int Seconds { get; set; }
    }
    
    public class PlayerHealthChangedData
    {
        public PlayerCharacterController  PlayerController { get; set; }
        public float NewHealth { get; set; }
        public float PreviousHealth { get; set; }
    }
}