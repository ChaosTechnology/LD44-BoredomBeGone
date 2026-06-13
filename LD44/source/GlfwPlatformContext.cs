using ChaosFramework.Collections;
using ChaosFramework.Components;
using ChaosFramework.Platform;
using Glfw = OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL;
using System;

namespace LD44
{
    public unsafe class GlfwPlatformContext
        : PlatformContext
        , GlContext
        , MessageQueue
    {
        class GlfwWindow : Window
        {
            static GlfwWindow _instance;
            public static GlfwWindow instance => _instance ??= new GlfwWindow();
            public readonly Glfw.Window* window;

            int Window.width => 800;
            int Window.height => 600;

            public GlfwWindow()
            {
                window = Glfw.GLFW.CreateWindow(800, 600, "GLFW Raw Window", (Glfw.Monitor*)IntPtr.Zero, (Glfw.Window*)IntPtr.Zero);
                Glfw.GLFW.MakeContextCurrent(window);
                Glfw.GLFW.ShowWindow(window);
                Glfw.GLFW.SetInputMode(window, Glfw.CursorStateAttribute.Cursor, Glfw.CursorModeValue.CursorDisabled);
            }

            void Window.Present()
            {
                Glfw.GLFW.MakeContextCurrent(window);
                Glfw.GLFW.SwapBuffers(window);
            }
        }

        public readonly Game.LD44Keyboard keyboard;
        public readonly Game.LD44Mouse mouse;

        public GlfwPlatformContext(Game.LD44Keyboard keyboard, Game.LD44Mouse mouse)
        {
            this.keyboard = keyboard;
            this.mouse = mouse;
            Glfw.GLFW.Init();
        }

        public event Action Terminate;
        bool terminated = false;
        AdvancedLinkedList<GlfwWindow> windows = new AdvancedLinkedList<GlfwWindow>();

        GlContext PlatformContext.glContext => this;

        Window PlatformContext.CreateWindow()
        {
            GlfwWindow window = new GlfwWindow();
            windows.Add(window);
            return window;
        }

        void GlContext.Init()
        {
            GL.LoadBindings(new Glfw.GLFWBindingsContext());
        }

        void MessageQueue.ProcessMessages()
        {
            Glfw.GLFW.PollEvents();

            foreach (GlfwWindow window in windows)
                if (Glfw.GLFW.WindowShouldClose(window.window))
                    windows.RemoveCurrent();

            if (windows.empty)
            {
                if (!terminated)
                {
                    terminated = true;
                    Terminate?.Invoke();
                }
                return;
            }

            foreach (var key in Enum.GetValues<Glfw.Keys>())
                keyboard.isDown[key] = Glfw.GLFW.GetKey(windows.first.window, key) == Glfw.InputAction.Press;

            const double offset = 25;

            Glfw.GLFW.GetCursorPos(windows.first.window, out double x, out double y);
            Glfw.GLFW.SetCursorPos(windows.first.window, offset, offset);
            mouse.X += (float)(x - offset);
            mouse.Y += (float)(y - offset);

            mouse.leftButton = Glfw.GLFW.GetMouseButton(windows.first.window, Glfw.MouseButton.Left) == Glfw.InputAction.Press;
            mouse.rightButton = Glfw.GLFW.GetMouseButton(windows.first.window, Glfw.MouseButton.Right) == Glfw.InputAction.Press;
        }
    }
}
