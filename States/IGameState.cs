using TheAdventure.Models;
using Silk.NET.Maths;

namespace TheAdventure.States
{
    public interface IGameState
    {
        void Initialize(Engine engine);
        void HandleInput(Input inputManager, Engine engine, double deltaTime);
        void Update(Engine engine, double deltaTime);
        void Render(GameRenderer renderer, double deltaTime);
        void OnEnter(Engine engine);
        void OnExit(Engine engine);
    }
}

