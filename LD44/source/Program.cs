using System;
using System.Reflection;
using System.Windows.Forms;

namespace LD44
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            typeof(System.Globalization.CultureInfo).GetField("s_userDefaultCulture", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, System.Globalization.CultureInfo.InvariantCulture);
            System.Environment.CurrentDirectory = ChaosUtil.Platform.Windows.Paths.Application.GetExecutableDirectory();

            ChaosUtil.Reflection.AssemblyManager.RegisterAssemblies(
                typeof(Program).Assembly,
                typeof(ChaosFramework.Math.Vectors.Vector2f).Assembly,
                typeof(ChaosFramework.Graphics.OpenGl.GlStateTracker.RenderStateChange).Assembly
                );

            ChaosFramework.IO.ChaosIO.Init(
                typeof(Program).Assembly,
                typeof(ChaosFramework.Math.Matrix).Assembly,
                typeof(ChaosFramework.Graphics.OpenGl.Graphics).Assembly,
                typeof(ChaosFramework.Graphics.Text.GlyphDimensions).Assembly
                );

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Game.PrepareIO();

            Game g = new Game(new Game.WindowsMessageQueue(), new Form());

            bool hadSettings = System.IO.File.Exists(Settings.FILE);
            g.settings = hadSettings
                ? Settings.Load(Settings.FILE)
                : new Settings();
            if (!hadSettings)
                g.settings.Save(Settings.FILE);

            g.window.Icon = Properties.Resources.icon;
            g.window.Text = "Boredom Be Gone";
            g.window.MinimumSize = new System.Drawing.Size(800, 450);
            g.window.Size = new System.Drawing.Size(g.settings.deferredShaderSize.x, g.settings.deferredShaderSize.y);
            g.window.BackgroundImageLayout = ImageLayout.Stretch;
            g.window.BackgroundImage = new System.Drawing.Bitmap("Assets/LoadingScreen.png");
            g.window.Show();
            g.Run();
        }
    }
}
