namespace TinyTanks.Level
{
    public class TestingGameMode : GameModeBase
    {
        protected override void OnFixedUpdate()
        {
            respawnCanvas.gameObject.SetActive(localPlayer == null || localPlayer.tank.isDestroyed);
        }
    }
}