using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using TheAdventure.Scripting;

namespace TheAdventure;

public class Engine
{
    private readonly GameRenderer _renderer;
    private readonly Input _input;
    private readonly ScriptEngine _scriptEngine = new();

    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
#pragma warning disable 414 // Field is assigned but its value is never used
    private readonly Dictionary<int, Tile> _tileIdMap = new();

    private Level _currentLevel;
    private PlayerObject? _player;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    private GodModePickup? _godModePickup;
    private int? _godModeTextureId;
    private bool _godModeActive;
    private DateTimeOffset _godModeEndTime;

    private bool _playerDead = false;
    private DateTimeOffset? _deathTime = null;
    public bool ShouldQuit => _playerDead && _deathTime.HasValue && (DateTimeOffset.Now - _deathTime.Value).TotalSeconds > 2;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;

        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
    }

    private static bool RectsIntersect(Rectangle<int> a, Rectangle<int> b)
    {
        return a.Origin.X < b.Origin.X + b.Size.X && a.Origin.X + a.Size.X > b.Origin.X &&
               a.Origin.Y < b.Origin.Y + b.Size.Y && a.Origin.Y + a.Size.Y > b.Origin.Y;
    }

    public void SetupWorld()
    {
        _player = new(SpriteSheet.Load(_renderer, "Player.json", "Assets"), 100, 100);

        _godModeTextureId = _renderer.LoadTexture("Assets/GodMode.png", out var godModeTextureInfo);

        var levelContent = File.ReadAllText(Path.Combine("Assets", "terrain.tmj"));
        var level = JsonSerializer.Deserialize<Level>(levelContent);
        if (level == null)
        {
            throw new Exception("Failed to load level");
        }

        foreach (var tileSetRef in level.TileSets)
        {
            var tileSetContent = File.ReadAllText(Path.Combine("Assets", tileSetRef.Source));
            var tileSet = JsonSerializer.Deserialize<TileSet>(tileSetContent);
            if (tileSet == null)
            {
                throw new Exception("Failed to load tile set");
            }

            foreach (var tile in tileSet.Tiles)
            {
                tile.TextureId = _renderer.LoadTexture(Path.Combine("Assets", tile.Image), out _);
                _tileIdMap.Add(tile.Id!.Value, tile);
            }

            _loadedTileSets.Add(tileSet.Name, tileSet);
        }

        if (level.Width == null || level.Height == null)
        {
            throw new Exception("Invalid level dimensions");
        }

        if (level.TileWidth == null || level.TileHeight == null)
        {
            throw new Exception("Invalid tile dimensions");
        }

        _renderer.SetWorldBounds(new Rectangle<int>(0, 0, level.Width.Value * level.TileWidth.Value,
            level.Height.Value * level.TileHeight.Value));

        _currentLevel = level;

        _scriptEngine.LoadAll(Path.Combine("Assets", "Scripts"));
    }

    public void ProcessFrame()
    {
        if (_playerDead) return;

        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        if (_player == null)
        {
            return;
        }

        if (_godModeActive && currentTime > _godModeEndTime)
        {
            _godModeActive = false;
        }

        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;
        bool isAttacking = _input.IsKeyAPressed() && (up + down + left + right <= 1);
        bool addBomb = _input.IsKeyBPressed();

        _player.UpdatePosition(up, down, left, right, 48, 48, msSinceLastFrame);
        if (isAttacking)
        {
            _player.Attack();
        }

        // GodMode pickup: check center-to-center distance for collision
        if (_godModePickup != null && !_godModeActive)
        {
            int playerCenterX = _player.Position.X + 8;
            int playerCenterY = _player.Position.Y + 8;
            int pickupCenterX = _godModePickup.Position.X + 8;
            int pickupCenterY = _godModePickup.Position.Y + 8;
            int dx = playerCenterX - pickupCenterX;
            int dy = playerCenterY - pickupCenterY;
            if (dx * dx + dy * dy <= 8 * 8)
            {
                _godModeActive = true;
                _godModeEndTime = currentTime.AddSeconds(5);
                _gameObjects.Remove(_godModePickup.Id);
                _godModePickup = null;
            }
        }

        _scriptEngine.ExecuteAll(this);

        if (addBomb)
        {
            AddBomb(_player.Position.X, _player.Position.Y, false);
        }
    }

    public void RenderFrame()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        var playerPosition = _player!.Position;
        _renderer.CameraLookAt(playerPosition.X, playerPosition.Y);

        RenderTerrain();
        RenderAllObjects();

        _renderer.PresentFrame();
    }

    public void RenderAllObjects()
    {
        var toRemove = new List<int>();
        foreach (var gameObject in GetRenderables())
        {
            if (_godModeActive && gameObject is TemporaryGameObject bomb)
            {
                var playerRect = new Rectangle<int>(_player!.Position.X, _player.Position.Y, 48, 48);
                var bombRect = new Rectangle<int>(bomb.Position.X, bomb.Position.Y, 48, 48);
                if (RectsIntersect(playerRect, bombRect))
                {
                    _renderer.SetDrawColor(255, 0, 0, 255);
                }
                else
                {
                    toRemove.Add(bomb.Id);
                    continue;
                }
            }
            gameObject.Render(_renderer);
            if (gameObject is TemporaryGameObject { IsExpired: true } tempGameObject)
            {
                toRemove.Add(tempGameObject.Id);
            }
        }

        foreach (var id in toRemove)
        {
            _gameObjects.Remove(id, out var gameObject);

            if (_player == null)
            {
                continue;
            }

            if (!_godModeActive && gameObject is TemporaryGameObject tempGameObject)
            {
                var playerCenter = (X: _player.Position.X + 24, Y: _player.Position.Y + 24);
                var bombCenter = (X: tempGameObject.Position.X + 24, Y: tempGameObject.Position.Y + 24);
                var dx = playerCenter.X - bombCenter.X;
                var dy = playerCenter.Y - bombCenter.Y;
                if (dx * dx + dy * dy <= 8 * 8)
                {
                    _player.GameOver();
                    _playerDead = true;
                    _deathTime = DateTimeOffset.Now;
                }
            }
        }

        if (_godModeActive)
        {
            _renderer.RenderAura(_player.Position.X, _player.Position.Y, 16, 16);
        }
        _player?.Render(_renderer);
    }

    public void RenderTerrain()
    {
        foreach (var currentLayer in _currentLevel.Layers)
        {
            for (int i = 0; i < _currentLevel.Width; ++i)
            {
                for (int j = 0; j < _currentLevel.Height; ++j)
                {
                    int? dataIndex = j * currentLayer.Width + i;
                    if (dataIndex == null)
                    {
                        continue;
                    }

                    var currentTileId = currentLayer.Data[dataIndex.Value] - 1;
                    if (currentTileId == null)
                    {
                        continue;
                    }

                    var currentTile = _tileIdMap[currentTileId.Value];

                    var tileWidth = currentTile.ImageWidth ?? 0;
                    var tileHeight = currentTile.ImageHeight ?? 0;

                    var sourceRect = new Rectangle<int>(0, 0, tileWidth, tileHeight);
                    var destRect = new Rectangle<int>(i * tileWidth, j * tileHeight, tileWidth, tileHeight);
                    _renderer.RenderTexture(currentTile.TextureId, sourceRect, destRect);
                }
            }
        }
    }

    public IEnumerable<RenderableGameObject> GetRenderables()
    {
        foreach (var gameObject in _gameObjects.Values)
        {
            if (gameObject is RenderableGameObject renderableGameObject)
            {
                yield return renderableGameObject;
            }
        }
    }

    public (int X, int Y) GetPlayerPosition()
    {
        return _player!.Position;
    }

    public void AddBomb(int X, int Y, bool translateCoordinates = true)
    {
        var worldCoords = translateCoordinates ? _renderer.ToWorldCoordinates(X, Y) : new Vector2D<int>(X, Y);

        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "BombExploding.json", "Assets");
        spriteSheet.ActivateAnimation("Explode");

        TemporaryGameObject bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(bomb.Id, bomb);
    }

    public bool IsGodModeActive() => _godModeActive;

    public void SpawnGodModePickup(int x, int y)
    {
        if (_godModeTextureId == null || _godModePickup != null)
            return;
        _godModePickup = new GodModePickup(_godModeTextureId.Value, (x, y), 32);
        _gameObjects.Add(_godModePickup.Id, _godModePickup);
    }

    public bool IsGodModePickupPresent() => _godModePickup != null;
}
