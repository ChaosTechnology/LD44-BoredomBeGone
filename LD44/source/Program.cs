using System;
using System.Reflection;
using ChaosFramework.Input;
using ChaosFramework.Platform;
using OpenTK.Windowing.GraphicsLibraryFramework;

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
            PresentationContext window = platformContext.CreateFullscreen(title, platformContext.PrimaryMontior);
            Func<InputContext, InputDeviceHost> createHost = _ => new ChaosFramework.Input.RawInput.RawInputDeviceHost(_);
#else
            GlfwPlatformContext platformContext = new GlfwPlatformContext();
            platformContext.errorHandler.AddHandler(SettingIconNotSupportedHandler);
            PresentationContext window = platformContext.CreateFullscreen(title, platformContext.PrimaryMonitor);
            Func<InputContext, InputDeviceHost> createHost = context => new ChaosFramework.Input.OpenTk.DeviceHost(context, ((GlfwFullscreen)window).window);
#endif

            window.SetIcon(
                new ApplicationIcon(
                    ApplicationIcon.IconFormat.ico,
                    () => Properties.Resources.ResourceManager.GetStream(nameof(Properties.Resources.icon)))
                    );

            // TODO: render loading screen again
            Game g = new Game(platformContext, window, createHost);
            g.settings = settings;
            g.Run();
        }

#if !OS_WINDOWS
        static bool SettingIconNotSupportedHandler(ErrorCode errorCode, string message)
        {
            // here's hoping that this error message never gets localized
            if (errorCode == ErrorCode.FeatureUnavailable && message.Contains("The platform does not support setting the window icon"))
            {
                Console.WriteLine("Couldn't set icon for Glfw presentation context.");
                Console.WriteLine(message);
                return true;
            }

            return false;
        }
#endif
    }
}
