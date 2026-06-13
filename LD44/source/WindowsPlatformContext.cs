#if OS_WINDOWS
using ChaosFramework.Components;
using ChaosFramework.Platform;
using OpenTK.GLControl;
using System;
using System.Windows.Forms;

namespace LD44
{
    public class WindowsPlatformContext
        : MessageQueue
        , PlatformContext
        , GlContext
    {
        class WindowsWindow : Window
        {
            public readonly Form form;
            readonly GLControl control;

            int Window.width => form.Width;
            int Window.height => form.Height;

            public WindowsWindow(Form form)
            {
                this.form = form;
                control = new GLControl();
                control.Bounds = form.ClientRectangle;
                control.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
                form.Controls.Add(control);
            }

            void Window.Present()
            {
                control.Context.MakeCurrent();
                control.SwapBuffers();
            }
        }

        public Form GetForm(Window window) => (window as WindowsWindow)?.form;

        Window PlatformContext.CreateWindow()
        {
            var form = new Form();
            var window = new WindowsWindow(form);
            form.Show();
            form.FormClosing += RaiseTerminate;
            return window;
        }

        GlContext PlatformContext.glContext => this;

        public event Action Terminate;

        void MessageQueue.ProcessMessages()
        {
            Application.DoEvents();
        }

        void GlContext.Init()
        {
        }

        void RaiseTerminate(object _, EventArgs __)
            => Terminate?.Invoke();
    }
}
#endif
