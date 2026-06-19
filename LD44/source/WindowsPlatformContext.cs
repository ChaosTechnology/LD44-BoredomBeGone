#if OS_WINDOWS
using ChaosFramework.Platform;
using OpenTK.GLControl;
using System;
using System.Windows.Forms;

namespace LD44
{
    public class WindowsPlatformContext
        : PlatformContext
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
                form.FormBorderStyle = FormBorderStyle.None;
                form.Bounds = Screen.PrimaryScreen.Bounds;
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

        Overhead PlatformContext.messageQueue => Overhead;

        GlContext PlatformContext.glContext => this;

        public event Action Terminate;

        void GlContext.Init()
        {
        }

        void Overhead()
        {
            Application.DoEvents();
            Cursor.Hide();
            Cursor.Position = Screen.PrimaryScreen.Bounds.Location;
        }

        void RaiseTerminate(object _, EventArgs __)
            => Terminate?.Invoke();
    }
}
#endif
