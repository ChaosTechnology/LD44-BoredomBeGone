using ChaosFramework.Input;
using ChaosFramework.Platform;
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
            System.Environment.CurrentDirectory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

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

#if OS_WINDOWS
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#endif
            Game.PrepareIO();

            bool hadSettings = System.IO.File.Exists(Settings.FILE);
            Settings settings = hadSettings
                ? Settings.Load(Settings.FILE)
                : new Settings();
            if (!hadSettings)
                settings.Save(Settings.FILE);

#if OS_WINDOWS
            WindowsPlatformContext platformContext = new WindowsPlatformContext();
            Window window = ((PlatformContext)platformContext).CreateWindow();
            Form form = platformContext.GetForm(window);
            form.Icon = Properties.Resources.icon;
            form.Text = "Boredom Be Gone";
            form.MinimumSize = new System.Drawing.Size(800, 450);
            form.Size = new System.Drawing.Size(settings.deferredShaderSize.x, settings.deferredShaderSize.y);
            form.BackgroundImageLayout = ImageLayout.Stretch;
            form.BackgroundImage = new System.Drawing.Bitmap("Assets/LoadingScreen.png");

            Game g = new Game(platformContext, window);
#else
            GlfwPlatformContext platformContext = new GlfwPlatformContext();
            GlfwPlatformContext.GlfwWindow window = platformContext.CreateWindow();
            Func<InputContext, InputDeviceHost> createHost = _ => new ChaosFramework.Input.OpenTk.DeviceHost(_, window.window);
            Game g = new Game(platformContext, window, createHost);
#endif

            g.settings = settings;
            g.Run();
        }
    }
}
