/// <summary>
/// An interface for scripts who want to use the GameplayManager.
/// </summary>
public interface GameplayManagerBehavior
{
    /// <summary>
    /// Callback for when the GameplayManager is loaded. See SceneDelegate#GetGameplayManager for more details.
    /// </summary>
    /// <param name="gameplayManager"></param>
    void GameplayManagerLoaded(GameplayManager gameplayManager);
}
