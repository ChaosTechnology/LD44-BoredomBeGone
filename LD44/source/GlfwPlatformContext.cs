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

            int w, h;
            int Window.width => w;
            int Window.height => h;

            public GlfwWindow()
            {
                Glfw.Monitor* monitor = Glfw.GLFW.GetPrimaryMonitor();
                Glfw.VideoMode* vm = Glfw.GLFW.GetVideoMode(monitor);
                window = Glfw.GLFW.CreateWindow(w = vm->Width, h = vm->Height, "LD44-BoredomBeGone", monitor, (Glfw.Window*)IntPtr.Zero);
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
        readonly Glfw.GLFWCallbacks.MouseButtonCallback mouseCallback;
        readonly Glfw.GLFWCallbacks.KeyCallback keyCallback;

        public GlfwPlatformContext(Game.LD44Keyboard keyboard, Game.LD44Mouse mouse)
        {
            mouseCallback = MouseCallback;
            keyCallback = KeyCallback;
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
            Glfw.GLFW.SetMouseButtonCallback(window.window, mouseCallback);
            Glfw.GLFW.SetKeyCallback(window.window, keyCallback);
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

            const double offset = 25;
            Glfw.GLFW.GetCursorPos(windows.first.window, out double x, out double y);
            Glfw.GLFW.SetCursorPos(windows.first.window, offset, offset);
            mouse.X += (float)(x - offset);
            mouse.Y += (float)(y - offset);
        }

        void MouseCallback(Glfw.Window* wnd, Glfw.MouseButton btn, Glfw.InputAction action, Glfw.KeyModifiers modifiers)
        {
            if (wnd == windows.first.window)
                switch(btn)
                {
                    case Glfw.MouseButton.Button1:
                        mouse.leftButton = action != Glfw.InputAction.Release;
                        break;
                    case Glfw.MouseButton.Button2:
                        mouse.rightButton = action != Glfw.InputAction.Release;
                        break;
                }
        }

        void KeyCallback(Glfw.Window* wnd, Glfw.Keys key, int scanCode, Glfw.InputAction action, Glfw.KeyModifiers mods)
        {
            if (wnd == windows.first.window)
                keyboard.isDown[key] = action != Glfw.InputAction.Release;
        }
    }
}
