using ChaosFramework.Collections;
using ChaosFramework.Components;
using ChaosFramework.Platform;
using Glfw = OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL;
using System;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Mathematics;

namespace LD44
{
    public unsafe class GlfwPlatformContext
        : PlatformContext
        , GlContext
    {
        public class GlfwWindow : Window
        {
            public readonly NativeWindow window;

            int w, h;
            int Window.width => w;
            int Window.height => h;

            public GlfwWindow()
            {
                Glfw.Monitor* monitor = Glfw.GLFW.GetPrimaryMonitor();
                Glfw.VideoMode* vm = Glfw.GLFW.GetVideoMode(monitor);
                window = new NativeWindow(new NativeWindowSettings()
                {
                    Size = new Vector2i(w = vm->Width, h = vm->Height),
                    Title = "LD44-BoredomBeGone",
                    WindowState = WindowState.Fullscreen,
                    StartVisible = true,
                    APIVersion = new Version(3, 3)
                });
                Glfw.GLFW.MakeContextCurrent(window.WindowPtr);
                Glfw.GLFW.ShowWindow(window.WindowPtr);
                Glfw.GLFW.SetInputMode(window.WindowPtr, Glfw.CursorStateAttribute.Cursor, Glfw.CursorModeValue.CursorDisabled);
            }

            void Window.Present()
            {
                Glfw.GLFW.MakeContextCurrent(window.WindowPtr);
                Glfw.GLFW.SwapBuffers(window.WindowPtr);
            }
        }

        Overhead PlatformContext.messageQueue => PerformOverhead;

        public GlfwPlatformContext()
        {
            Glfw.GLFW.Init();
        }

        public event Action Terminate;
        bool terminated = false;
        AdvancedLinkedList<GlfwWindow> windows = new AdvancedLinkedList<GlfwWindow>();

        GlContext PlatformContext.glContext => this;

        public GlfwWindow CreateWindow()
        {
            GlfwWindow window = new GlfwWindow();
            windows.Add(window);
            return window;
        }

        Window PlatformContext.CreateWindow()
            => CreateWindow();

        void GlContext.Init()
        {
            GL.LoadBindings(new Glfw.GLFWBindingsContext());
        }

        void PerformOverhead()
        {
            Glfw.GLFW.PollEvents();

            foreach (GlfwWindow window in windows)
                if (Glfw.GLFW.WindowShouldClose(window.window.WindowPtr))
                    windows.RemoveCurrent();

            if (windows.empty && !terminated)
            {
                terminated = true;
                Terminate?.Invoke();
            }
        }
    }
}
