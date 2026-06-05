using ChaosFramework.Components;
using ChaosFramework.Platform;
using OpenTK.GLControl;
using System;
using System.Windows.Forms;

namespace LD44
{
    public class WindowsPlatformContext(Form window)
        : MessageQueue
        , PlatformContext
    {
        class WindowsWindow(Form window) : PrimaryWindow
        {
            int PrimaryWindow.width => window.Width;
            int PrimaryWindow.height => window.Height;
        }

        PrimaryWindow PlatformContext.primaryWindow { get; } = new WindowsWindow(window);

        public event Action Terminate;

        GLControl control;

        void MessageQueue.ProcessMessages()
        {
            Application.DoEvents();
        }

        void PlatformContext.Setup()
        {
            window.Show();
            window.FormClosing += RaiseTerminate;

            control = new GLControl();
            control.Bounds = window.ClientRectangle;
            control.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            window.Controls.Add(control);
            control.Context.MakeCurrent();
        }

        void PlatformContext.Present()
        {
            control.SwapBuffers();
        }

        void RaiseTerminate(object _, EventArgs __)
            => Terminate?.Invoke();
    }
}
