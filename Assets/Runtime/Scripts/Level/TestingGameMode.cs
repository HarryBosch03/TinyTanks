using TinyTanks.Tanks;

namespace TinyTanks.Level
{
    public class TestingGameMode : GameModeBase
    {
        protected override void OnFixedUpdate()
        {
            respawnCanvas.gameObject.SetActive(TankInput.localPlayer == null || TankInput.localPlayer.tank.isDestroyed);
        }
    }
}