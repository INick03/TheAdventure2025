// Engine.cs
using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using TheAdventure.Scripting;
using TheAdventure.States;

namespace TheAdventure;

public class Engine
{
    public readonly GameRenderer Renderer;
    public readonly Input Input;
    private readonly ScriptEngine _scriptEngine = new();

    public readonly Dictionary<int, GameObject> GameObjects = new();
    public readonly Dictionary<string, TileSet> LoadedTileSets = new();
    public readonly Dictionary<int, Tile> TileIdMap = new();
    public Level CurrentLevel = new();
    public PlayerObject? Player;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    private IGameState? _currentState;
    private IGameState? _nextState;
    public bool IsRunning { get; private set; } = true;

    public Engine(GameRenderer renderer, Input input)
    {
        Renderer = renderer;
        Input = input;
        Input.OnMouseClick += (_, coords) =>
        {
            AddBomb(coords.x, coords.y);
        };
    }

    public void Initialize()
    {
        SetupWorld();
        ChangeState(new PlayingState(this));
    }

    public void SetupWorld()
    {
        TileIdMap.Clear();
        LoadedTileSets.Clear();
        GameObjects.Clear();
        Player = new(SpriteSheet.Load(Renderer, "Player.json", "Assets"), 100, 100);
        var levelContent = File.ReadAllText(Path.Combine("Assets", "terrain.tmj"));
        var level = JsonSerializer.Deserialize<Level>(levelContent);
        if (level == null)
            throw new Exception("Failed to load level");
        foreach (var tileSetRef in level.TileSets)
        {
            var tileSetContent = File.ReadAllText(Path.Combine("Assets", tileSetRef.Source));
            var tileSet = JsonSerializer.Deserialize<TileSet>(tileSetContent);
            if (tileSet == null)
                throw new Exception("Failed to load tile set");
            foreach (var tile in tileSet.Tiles)
            {
                tile.TextureId = Renderer.LoadTexture(Path.Combine("Assets", tile.Image), out _);
                TileIdMap.Add(tile.Id!.Value, tile);
            }
            LoadedTileSets.Add(tileSet.Name, tileSet);
        }
        if (level.Width == null || level.Height == null)
            throw new Exception("Invalid level dimensions");
        if (level.TileWidth == null || level.TileHeight == null)
            throw new Exception("Invalid tile dimensions");
        Renderer.SetWorldBounds(new Rectangle<int>(0, 0, level.Width.Value * level.TileWidth.Value,
            level.Height.Value * level.TileHeight.Value));
        CurrentLevel = level;
        _scriptEngine.LoadAll(Path.Combine("Assets", "Scripts"));
    }

    public void GameLoop()
    {
        var currentTime = DateTimeOffset.Now;
        var deltaTime = (currentTime - _lastUpdate).TotalSeconds;
        _lastUpdate = currentTime;
        if (_nextState != null)
        {
            _currentState?.OnExit(this);
            _currentState = _nextState;
            _nextState = null;
            _currentState?.Initialize(this);
            _currentState?.OnEnter(this);
        }
        _currentState?.HandleInput(Input, this, deltaTime);
        _currentState?.Update(this, deltaTime);
        Renderer.SetDrawColor(0, 0, 0, 255);
        Renderer.ClearScreen();
        if (Player != null)
            Renderer.CameraLookAt(Player.Position.X, Player.Position.Y);
        _currentState?.Render(Renderer, deltaTime);
        Renderer.PresentFrame();
    }

    public void ChangeState(IGameState newState)
    {
        _nextState = newState;
    }

    public void QuitGame()
    {
        IsRunning = false; 
    }

    public void AddBomb(int x, int y, bool translateCoordinates = true)
    {
        var worldCoords = translateCoordinates ? Renderer.ToWorldCoordinates(x, y) : new Vector2D<int>(x, y);
        SpriteSheet spriteSheet = SpriteSheet.Load(Renderer, "BombExploding.json", "Assets");
        spriteSheet.ActivateAnimation("Explode");
        TemporaryGameObject bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y));
        GameObjects.Add(bomb.Id, bomb);
    }

    public IEnumerable<RenderableGameObject> GetRenderablesFromEngine()
    {
        foreach (var gameObject in GameObjects.Values)
        {
            if (gameObject is RenderableGameObject renderableGameObject)
                yield return renderableGameObject;
        }
    }

    public (int X, int Y) GetPlayerPositionFromEngine()
    {
        if (Player == null) return (0,0);
        return Player.Position;
    }

    public void ExecuteScripts()
    {
        _scriptEngine.ExecuteAll(this);
    }

    public void CheckTemporaryObjectsAndPlayerDeath()
    {
        var toRemove = new List<int>();
        foreach (var gameObject in GameObjects.Values)
        {
            if (gameObject is TemporaryGameObject { IsExpired: true } tempGameObject)
                toRemove.Add(tempGameObject.Id);
        }
        foreach (var id in toRemove)
        {
            GameObjects.Remove(id, out var gameObject);
            if (Player == null || gameObject == null)
                continue;
            var tempGameObject = (TemporaryGameObject)gameObject;
            var deltaX = Math.Abs(Player.Position.X - tempGameObject.Position.X);
            var deltaY = Math.Abs(Player.Position.Y - tempGameObject.Position.Y);
            if (deltaX < 32 && deltaY < 32)
                Player.GameOver();
        }
    }
}
