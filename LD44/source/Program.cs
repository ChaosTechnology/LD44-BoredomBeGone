using ChaosFramework.Input;
using ChaosFramework.Platform;
using System;
using System.Reflection;
using System.Linq;


#if !OS_WINDOWS
using ChaosFramework.Platform.Glfw;
#else
using ChaosFramework.Platform.WinForms;
#endif

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
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
#endif
            Game.PrepareIO();

            bool hadSettings = System.IO.File.Exists(Settings.FILE);
            Settings settings = hadSettings
                ? Settings.Load(Settings.FILE)
                : new Settings();
            if (!hadSettings)
                settings.Save(Settings.FILE);

            string title = "LD44-BoredomBeGone";
#if OS_WINDOWS
            WinFormsPlatformContext platformContext = new WinFormsPlatformContext();
            WinFormsFullscreen window = platformContext.CreateFullscreen(title, platformContext.PrimaryMontior);
            window.SetIcon(Properties.Resources.icon);
            Func<InputContext, InputDeviceHost> createHost = _ => new ChaosFramework.Input.RawInput.RawInputDeviceHost(_);
#else
            GlfwPlatformContext platformContext = new GlfwPlatformContext();
            GlfwFullscreen window = platformContext.CreateFullscreen(title, platformContext.PrimaryMonitor);
            Func<InputContext, InputDeviceHost> createHost = context => new ChaosFramework.Input.OpenTk.DeviceHost(context, window.window);
#endif

            // TODO: render loading screen again
            Game g = new Game(platformContext, window, createHost);
            g.settings = settings;
            g.Run();
        }
    }
}
