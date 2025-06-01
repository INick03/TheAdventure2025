// States/GameOverState.cs
using TheAdventure.Models;
using Silk.NET.Maths;

namespace TheAdventure.States
{
    public class GameOverState : IGameState
    {
        private Engine _engineRef;
        private Rectangle<int> _restartButtonRect;
        private Rectangle<int> _quitButtonRect;

        private int _screenWidth;
        private int _screenHeight;

        private int? _gameOverImageTextureId = null;
        private int _gameOverImageWidth = 0;
        private int _gameOverImageHeight = 0;

        public GameOverState(Engine engine)
        {
            _engineRef = engine;
        }

        public void Initialize(Engine engine)
        {
            _screenWidth = _engineRef.Renderer.GetCameraViewSize().X;
            _screenHeight = _engineRef.Renderer.GetCameraViewSize().Y;
            _engineRef.Renderer.CenterCameraOnScreen(_screenWidth, _screenHeight);
            string imagePath = "Assets/game_over.png";
            if (System.IO.File.Exists(imagePath))
            {
                _gameOverImageTextureId = _engineRef.Renderer.LoadTexture(imagePath, out var texInfo);
                _gameOverImageWidth = texInfo.Width;
                _gameOverImageHeight = texInfo.Height;
            }
            else
            {
                _gameOverImageTextureId = null;
            }
        }

        public void HandleInput(Input inputManager, Engine engine, double deltaTime)
        {
            if (inputManager.IsMouseButtonPressedThisFrame(MouseButton.Left))
            {
                var mousePos = inputManager.GetMousePosition();
                if (_gameOverImageTextureId.HasValue)
                {
                    int imgX = _screenWidth / 2 - _gameOverImageWidth / 2;
                    int imgY = _screenHeight / 2 - _gameOverImageHeight / 2;
                    var restartRect = new Rectangle<int>(imgX + 220, imgY + 170, 200, 60);
                    var quitRect = new Rectangle<int>(imgX + 220, imgY + 260, 200, 60);
                    if (mousePos.X >= restartRect.Origin.X && mousePos.X <= restartRect.Origin.X + restartRect.Size.X &&
                        mousePos.Y >= restartRect.Origin.Y && mousePos.Y <= restartRect.Origin.Y + restartRect.Size.Y)
                    {
                        try
                        {
                            engine.SetupWorld();
                            engine.ChangeState(new PlayingState(engine));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Exception during restart: {ex}");
                            throw;
                        }
                        return;
                    }
                    if (mousePos.X >= quitRect.Origin.X && mousePos.X <= quitRect.Origin.X + quitRect.Size.X &&
                        mousePos.Y >= quitRect.Origin.Y && mousePos.Y <= quitRect.Origin.Y + quitRect.Size.Y)
                    {
                        engine.QuitGame();
                        return;
                    }
                }
            }
        }

        public void Update(Engine engine, double deltaTime)
        {
        }

        public void Render(GameRenderer renderer, double deltaTime)
        {
            if (_gameOverImageTextureId.HasValue)
            {
                int imgX = _screenWidth / 2 - _gameOverImageWidth / 2;
                int imgY = _screenHeight / 2 - _gameOverImageHeight / 2;
                var destRect = new Silk.NET.Maths.Rectangle<int>(imgX, imgY, _gameOverImageWidth, _gameOverImageHeight);
                var srcRect = new Silk.NET.Maths.Rectangle<int>(0, 0, _gameOverImageWidth, _gameOverImageHeight);
                renderer.RenderTexture(_gameOverImageTextureId.Value, srcRect, destRect);
            }
        }

        public void OnEnter(Engine engine)
        {
            if (_screenWidth > 0 && _screenHeight > 0)
                _engineRef.Renderer.CenterCameraOnScreen(_screenWidth, _screenHeight);
        }

        public void OnExit(Engine engine)
        {
        }
    }
}

