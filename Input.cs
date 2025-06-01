// Input.cs
using Silk.NET.SDL;
using Silk.NET.Maths;
using System.Collections.Generic;

namespace TheAdventure;

public unsafe class Input
{
    private readonly Sdl _sdl;
    public EventHandler<(int x, int y)>? OnMouseClick;

    private uint _currentMouseStateBits;
    private uint _previousMouseStateBits;
    private int _mouseX;
    private int _mouseY;

    private HashSet<KeyCode> _keysPressedThisFrame = new();
    private HashSet<KeyCode> _keysReleasedThisFrame = new();
    private byte[] _currentKeyboardState;
    private byte[] _previousKeyboardState;

    public Input(Sdl sdl)
    {
        _sdl = sdl;
        int numKeys = (int)Scancode.NumScancodes;
        _currentKeyboardState = new byte[numKeys];
        _previousKeyboardState = new byte[numKeys];
    }

    private void UpdateKeyboardState()
    {
        _previousKeyboardState = (byte[])_currentKeyboardState.Clone();
        byte* sdlKeysStatePtr = _sdl.GetKeyboardState(null);
        for (int i = 0; i < _currentKeyboardState.Length; ++i)
        {
            _currentKeyboardState[i] = sdlKeysStatePtr[i];
        }
        _keysPressedThisFrame.Clear();
        _keysReleasedThisFrame.Clear();
        for (int i = 0; i < _currentKeyboardState.Length; i++)
        {
            if (_currentKeyboardState[i] == 1 && _previousKeyboardState[i] == 0)
            {
                _keysPressedThisFrame.Add((KeyCode)i);
            }
            else if (_currentKeyboardState[i] == 0 && _previousKeyboardState[i] == 1)
            {
                _keysReleasedThisFrame.Add((KeyCode)i);
            }
        }
    }

    private Scancode ToScancode(KeyCode key) => (Scancode)key;

    public bool IsKeyDown(KeyCode key) => _currentKeyboardState[(int)ToScancode(key)] == 1;
    public bool IsKeyPressedThisFrame(KeyCode key) => _keysPressedThisFrame.Contains(key);
    public bool IsKeyReleasedThisFrame(KeyCode key) => _keysReleasedThisFrame.Contains(key);
    public bool IsLeftPressed() => IsKeyDown(KeyCode.Left);
    public bool IsRightPressed() => IsKeyDown(KeyCode.Right);
    public bool IsUpPressed() => IsKeyDown(KeyCode.Up);
    public bool IsDownPressed() => IsKeyDown(KeyCode.Down);
    public bool IsKeyAPressed() => IsKeyDown(KeyCode.A);
    public bool IsKeyBPressed() => IsKeyDown(KeyCode.B);

    private void UpdateMouseState()
    {
        _previousMouseStateBits = _currentMouseStateBits;
        _currentMouseStateBits = _sdl.GetMouseState(ref _mouseX, ref _mouseY);
    }

    public Vector2D<int> GetMousePosition() => new Vector2D<int>(_mouseX, _mouseY);

    private uint GetSdlMouseButtonMask(TheAdventure.MouseButton button)
    {
        const int SDL_BUTTON_LEFT = 1;
        const int SDL_BUTTON_MIDDLE = 2;
        const int SDL_BUTTON_RIGHT = 3;
        const int SDL_BUTTON_X1 = 4;
        const int SDL_BUTTON_X2 = 5;
        return button switch
        {
            TheAdventure.MouseButton.Left => 1u << (SDL_BUTTON_LEFT - 1),
            TheAdventure.MouseButton.Middle => 1u << (SDL_BUTTON_MIDDLE - 1),
            TheAdventure.MouseButton.Right => 1u << (SDL_BUTTON_RIGHT - 1),
            TheAdventure.MouseButton.X1 => 1u << (SDL_BUTTON_X1 - 1),
            TheAdventure.MouseButton.X2 => 1u << (SDL_BUTTON_X2 - 1),
            _ => 0
        };
    }

    public bool IsMouseButtonDown(TheAdventure.MouseButton button)
    {
        uint mask = GetSdlMouseButtonMask(button);
        return (_currentMouseStateBits & mask) != 0;
    }

    public bool IsMouseButtonPressedThisFrame(TheAdventure.MouseButton button)
    {
        uint mask = GetSdlMouseButtonMask(button);
        bool currentlyPressed = (_currentMouseStateBits & mask) != 0;
        bool previouslyPressed = (_previousMouseStateBits & mask) != 0;
        return currentlyPressed && !previouslyPressed;
    }

    public bool IsMouseButtonReleasedThisFrame(TheAdventure.MouseButton button)
    {
        uint mask = GetSdlMouseButtonMask(button);
        bool currentlyPressed = (_currentMouseStateBits & mask) != 0;
        bool previouslyPressed = (_previousMouseStateBits & mask) != 0;
        return !currentlyPressed && previouslyPressed;
    }

    public bool ProcessInput()
    {
        UpdateKeyboardState();
        UpdateMouseState();
        Event ev = new Event();
        while (_sdl.PollEvent(ref ev) != 0)
        {
            if (ev.Type == (uint)EventType.Quit)
                return true;
            switch (ev.Type)
            {
                case (uint)EventType.Windowevent:
                    break;
                case (uint)EventType.Mousebuttondown:
                    const byte SDL_BUTTON_LEFT_CONST = 1;
                    if (ev.Button.Button == SDL_BUTTON_LEFT_CONST)
                        OnMouseClick?.Invoke(this, (ev.Button.X, ev.Button.Y));
                    break;
            }
        }
        return false;
    }
}

