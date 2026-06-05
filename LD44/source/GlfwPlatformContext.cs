using ChaosFramework.Platform;
using ChaosFramework.Components;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace LD44
{
    public unsafe class GlfwPlatformContext(Game.LD44Keyboard keyboard, Game.LD44Mouse mouse)
        : PlatformContext
        , MessageQueue
    {
        public class WindowsPrimaryWindow : PrimaryWindow
        {
            static WindowsPrimaryWindow _instance;
            public static WindowsPrimaryWindow instance => _instance ??= new WindowsPrimaryWindow();

            public readonly Window* window;
            WindowsPrimaryWindow()
            {
                GLFW.Init();
                window = GLFW.CreateWindow(800, 600, "GLFW Raw Window", (Monitor*)IntPtr.Zero, (Window*)IntPtr.Zero);
                GLFW.MakeContextCurrent(window);
                GLFW.ShowWindow(window);
            }

            int PrimaryWindow.width => 800;
            int PrimaryWindow.height => 600;
        }

        public event Action Terminate;
        bool terminated = false;

        public WindowsPrimaryWindow primaryWindow => WindowsPrimaryWindow.instance;
        PrimaryWindow PlatformContext.primaryWindow => primaryWindow;

        void PlatformContext.Setup()
        {
            _ = primaryWindow;
        }

        public void Present()
        {
            GLFW.SwapBuffers(primaryWindow.window);
        }

        void MessageQueue.ProcessMessages()
        {
            GLFW.PollEvents();

            if (GLFW.WindowShouldClose(primaryWindow.window) && !terminated)
            {
                terminated = true;
                Terminate?.Invoke();
                return;
            }

            foreach (var key in Enum.GetValues<Keys>())
                keyboard.isDown[key] = GLFW.GetKey(primaryWindow.window, key) == InputAction.Press;

            const double offset = 25;

            GLFW.GetCursorPos(primaryWindow.window, out double x, out double y);
            GLFW.SetCursorPos(primaryWindow.window, offset, offset);
            mouse.X += (float)(x - offset);
            mouse.Y += (float)(y - offset);

            mouse.leftButton = GLFW.GetMouseButton(primaryWindow.window, MouseButton.Left) == InputAction.Press;
            mouse.rightButton = GLFW.GetMouseButton(primaryWindow.window, MouseButton.Right) == InputAction.Press;
        }
    }
}
