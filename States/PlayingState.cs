// States/PlayingState.cs
using Silk.NET.Maths;
using TheAdventure.Models;

namespace TheAdventure.States
{
    public class PlayingState : IGameState
    {
        private Engine _engineRef; // Make non-nullable, pass via constructor
        private PlayerObject? _player;

        // Constructor
        public PlayingState(Engine engine)
        {
            _engineRef = engine;
        }

        public void Initialize(Engine engine) // engine param still here due to interface
        {
            // _engineRef is already set
            _player = _engineRef.Player;

            if (_player == null)
            {
                 _engineRef.SetupWorld(); // This reloads level, player etc.
                 _player = _engineRef.Player;
            }
            // Reset player state if already exists (e.g. on restart)
            _player?.SetState(PlayerObject.PlayerState.Idle, PlayerObject.PlayerStateDirection.Down);
            // Consider resetting player position here too if not done in Player.SetState or SetupWorld
            // _player?.SetPosition(100,100); // Example initial position
            _engineRef.GameObjects.Clear(); // Clear temporary objects like bombs
        }

        public void HandleInput(Input inputManager, Engine engine, double deltaTime)
        {
            if (_player == null || _player.State.State == PlayerObject.PlayerState.GameOver) return;

            double up = inputManager.IsUpPressed() ? 1.0 : 0.0;
            double down = inputManager.IsDownPressed() ? 1.0 : 0.0;
            double left = inputManager.IsLeftPressed() ? 1.0 : 0.0;
            double right = inputManager.IsRightPressed() ? 1.0 : 0.0;
            bool isAttacking = inputManager.IsKeyAPressed() && (up + down + left + right <= 0.1);
            bool addBombInput = inputManager.IsKeyBPressed();

            _player.UpdatePosition(up, down, left, right, 48, 48, deltaTime * 1000.0);

            if (isAttacking)
            {
                _player.Attack();
                var bombsToRemove = new List<int>();
                foreach (var obj in engine.GameObjects.Values)
                {
                    if (obj is TheAdventure.Models.TemporaryGameObject bomb)
                    {
                        int px = _player.Position.X;
                        int py = _player.Position.Y;
                        int bx = bomb.Position.X;
                        int by = bomb.Position.Y;
                        int range = 32; // Attack range in pixels
                        var dir = _player.State.Direction;
                        bool inRange = false;
                        switch (dir)
                        {
                            case PlayerObject.PlayerStateDirection.Up:
                                inRange = (by < py) && (Math.Abs(by - py) <= range) && (Math.Abs(bx - px) <= range);
                                break;
                            case PlayerObject.PlayerStateDirection.Down:
                                inRange = (by > py) && (Math.Abs(by - py) <= range) && (Math.Abs(bx - px) <= range);
                                break;
                            case PlayerObject.PlayerStateDirection.Left:
                                inRange = (bx < px) && (Math.Abs(bx - px) <= range) && (Math.Abs(by - py) <= range);
                                break;
                            case PlayerObject.PlayerStateDirection.Right:
                                inRange = (bx > px) && (Math.Abs(bx - px) <= range) && (Math.Abs(by - py) <= range);
                                break;
                        }
                        if (inRange)
                        {
                            bombsToRemove.Add(bomb.Id);
                        }
                    }
                }
                foreach (var id in bombsToRemove)
                {
                    engine.GameObjects.Remove(id);
                }
            }

            if (addBombInput)
            {
                engine.AddBomb(_player.Position.X, _player.Position.Y, false);
            }
        }

        public void Update(Engine engine, double deltaTime)
        {
            if (_player == null) return;

            engine.ExecuteScripts();
            engine.CheckTemporaryObjectsAndPlayerDeath();

            if (_player.State.State == PlayerObject.PlayerState.GameOver)
            {
                engine.ChangeState(new GameOverState(engine)); // Pass engine to constructor
            }
        }

        public void Render(GameRenderer renderer, double deltaTime)
        {
            if (_player == null) return; // Should not happen if initialized correctly

            // Set camera to follow player for this state
            renderer.CameraLookAt(_player.Position.X, _player.Position.Y);

            foreach (var currentLayer in _engineRef.CurrentLevel.Layers)
            {
                // Ensure Width is not null before using it in loop condition or calculation
                int layerWidth = currentLayer.Width ?? 0;
                if (layerWidth == 0) continue; // Skip layer if width is invalid

                for (int i = 0; i < layerWidth; ++i)
                {
                    for (int j = 0; j < (_engineRef.CurrentLevel.Height ?? 0); ++j) // Assuming Level.Height is also nullable
                    {
                        int dataIndex = j * layerWidth + i;
                        if (dataIndex >= currentLayer.Data.Count) continue; // Bounds check

                        int? rawTileId = currentLayer.Data[dataIndex];
                        if (!rawTileId.HasValue || rawTileId.Value == 0) continue; // Tiled uses 0 for empty tile

                        int tileGid = rawTileId.Value -1; // Tiled GIDs are 1-indexed for actual tiles
                         // You might need more sophisticated GID to tileset mapping if you have multiple tilesets influencing GID ranges
                        if (tileGid < 0 || !_engineRef.TileIdMap.TryGetValue(tileGid, out var currentTile))
                        {
                            continue;
                        }

                        var tileWidth = currentTile.ImageWidth ?? 0;
                        var tileHeight = currentTile.ImageHeight ?? 0;
                        if (tileWidth == 0 || tileHeight == 0) continue;

                        var sourceRect = new Rectangle<int>(0, 0, tileWidth, tileHeight);
                        var destRect = new Rectangle<int>(i * tileWidth, j * tileHeight, tileWidth, tileHeight);
                        renderer.RenderTexture(currentTile.TextureId, sourceRect, destRect);
                    }
                }
            }

            foreach (var gameObject in _engineRef.GetRenderablesFromEngine())
            {
                gameObject.Render(renderer);
            }

            _player.Render(renderer);
        }

        public void OnEnter(Engine engine)
        {
            Console.WriteLine("Entered Playing State");
        }

        public void OnExit(Engine engine)
        {
            Console.WriteLine("Exited Playing State");
        }
    }
}

