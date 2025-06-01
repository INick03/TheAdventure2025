using TheAdventure.Scripting;
using System;
using TheAdventure;

public class RandomGodModePickup : IScript
{
    DateTimeOffset _nextPickupTimestamp;
    private static readonly int PickupIntervalSeconds = 10;

    public void Initialize()
    {
        _nextPickupTimestamp = DateTimeOffset.UtcNow.AddSeconds(PickupIntervalSeconds);
    }

    public void Execute(Engine engine)
    {
        if (_nextPickupTimestamp < DateTimeOffset.UtcNow)
        {
            _nextPickupTimestamp = DateTimeOffset.UtcNow.AddSeconds(PickupIntervalSeconds);
            // Only spawn if not already present
            if (!engine.IsGodModePickupPresent())
            {
                var playerPos = engine.GetPlayerPosition();
                var pickupPosX = playerPos.X + Random.Shared.Next(-80, 80);
                var pickupPosY = playerPos.Y + Random.Shared.Next(-80, 80);
                engine.SpawnGodModePickup(pickupPosX, pickupPosY);
            }
        }
    }
}

