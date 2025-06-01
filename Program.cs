// Program.cs
using Silk.NET.SDL;
using Thread = System.Threading.Thread;
using TheAdventure.States;

namespace TheAdventure;

public static class Program
{
    public static void Main()
    {
        var sdl = new Sdl(new SdlContext());
        var sdlInitResult = sdl.Init(Sdl.InitVideo | Sdl.InitAudio | Sdl.InitEvents | Sdl.InitTimer |
                                     Sdl.InitGamecontroller |
                                     Sdl.InitJoystick);
        if (sdlInitResult < 0)
            throw new InvalidOperationException("Failed to initialize SDL.");
        using (var gameWindow = new GameWindow(sdl))
        {
            var input = new Input(sdl);
            var gameRenderer = new GameRenderer(sdl, gameWindow);
            var engine = new Engine(gameRenderer, input);
            engine.Initialize();
            while (engine.IsRunning)
            {
                bool quitSignalFromInput = input.ProcessInput();
                if (quitSignalFromInput)
                    engine.QuitGame();
                if (!engine.IsRunning) break;
                engine.GameLoop();
                Thread.Sleep(13);
            }
        }
        sdl.Quit();
    }
}

