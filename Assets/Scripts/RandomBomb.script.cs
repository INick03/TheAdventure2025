using TheAdventure.Scripting;
using System;
using TheAdventure;

public class RandomBomb : IScript
{
    DateTimeOffset _nextBombTimestamp;
    private static readonly int BombRadius = 50;
    private static readonly Random _rand = new();

    public void Initialize()
    {
        _nextBombTimestamp = DateTimeOffset.UtcNow.AddSeconds(_rand.Next(2, 5));
    }

    public void Execute(Engine engine)
    {
        if (_nextBombTimestamp < DateTimeOffset.UtcNow)
        {
            _nextBombTimestamp = DateTimeOffset.UtcNow.AddSeconds(_rand.Next(2, 5));
            var playerPos = engine.GetPlayerPosition();
            // Consistent random spawn in a circle
            double angle = _rand.NextDouble() * Math.PI * 2;
            double radius = BombRadius * Math.Sqrt(_rand.NextDouble());
            int bombPosX = playerPos.X + (int)(radius * Math.Cos(angle));
            int bombPosY = playerPos.Y + (int)(radius * Math.Sin(angle));
            engine.AddBomb(bombPosX, bombPosY, false);
        }
    }
}

