using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Components;
using ChaosFramework.IO.Streams;
using ChaosFramework.IO.Streams.Sources;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Physics;
using ChaosFramework.Sound;
using ChaosFramework.Sound.OpenAL;
using ChaosUtil.Primitives;
using ChaosUtil.Serialization.Text;
using System.Windows.Forms;
using OpenTK.Graphics.OpenGL;

namespace LD44
{
    public class Game : BaseGame
    {
        public class WindowsMessageQueue : MessageQueue
        {
            void MessageQueue.ProcessMessages()
            {
                Application.DoEvents();
            }
        }

        public static readonly StreamSource assetSource = new FileStreamSource(new System.IO.DirectoryInfo("Assets"));

        internal static void PrepareIO()
            =>  Parse.AddParser<Vector2i>(Parsers.ParseVector2i);

        public AnimationContainer animations;
        public MaterialContainer materials;
        public TextureContainer textures;
        public ShaderContainer shaders;
        public ShaderCodeContainer shaderCode;
        public SoundDataContainer samples;
        public ShapeContainer shapes;
        public MeshContainer meshes;
        public FontContainer fonts;
        public Graphics graphics;
        public Settings settings;
        public Audio audio;

        Music music;

        bool preventRedrawOnResize = false;

        public OpenTK.Input.KeyboardState keyboard;
        public OpenTK.Input.MouseState mouse;

        public readonly MessageQueue messageQueue;
        public readonly Form window;

        bool lockF11;

        public Game(WindowsMessageQueue messageQueue, Form window)
            : base(messageQueue)
        {
            this.messageQueue = messageQueue;
            this.window = window;
            System.Windows.Forms.Cursor.Hide();
            window.Cursor.Dispose();
            window.FormClosing += Terminate;
        }

        public override void LoadGame()
        {
            base.LoadGame();
            gameLoop = new ChaosFramework.Components.GameLoop.CappedVariableTimeLoop(messageQueue, settings.maxFPS);

            audio = new Audio();
            samples = new SoundDataContainer(assetSource, false);
            music = new Music(audio, assetSource.OpenRead("Music/music.ogg"));
            music.PlayLoop(1);

            graphics = new Graphics(window, 3, 3);
            (fonts = new FontContainer(assetSource, graphics, false)).LoadDirectory("Fonts", new[] { ".chf2" }, true, null);
            (textures = new TextureContainer(assetSource, graphics.dispatcher, false)).LoadDirectory("Textures", new[] { ".png" }, true, null);
            (materials = new MaterialContainer(assetSource, graphics, textures, false)).LoadDirectory("Materials", new[] { ".mat" }, true, null);
            (meshes = new MeshContainer(assetSource, graphics.dispatcher, false)).LoadDirectory("Models", new[] { ".gmdl" }, true, null);
            (shaderCode = new ShaderCodeContainer(new StreamSourceCollection(StreamSources.shaderCode, assetSource))).LoadDirectory("Shaders", new[] { ".fx" }, true, null);
            (shaders = new ShaderContainer(assetSource, graphics, shaderCode)).LoadDirectory("Shaders", new[] { ".fx" }, true, null);
            (animations = new AnimationContainer(assetSource, false)).LoadDirectory("Animations", new[] { ".anim" }, true, null);
            (shapes = new ShapeContainer(assetSource)).LoadDirectory("Models", new[] { ".obj" }, true, null);
            scenes.Add(new WorldScene(this));

            window.BackgroundImage.Dispose();
            window.BackgroundImage = null;
        }

        protected override void Update()
        {
            keyboard = OpenTK.Input.Keyboard.GetState();
            mouse = OpenTK.Input.Mouse.GetState();

            bool toggleFullScreen = keyboard.IsKeyDown(OpenTK.Input.Key.F11);
            if (toggleFullScreen && !lockF11)
            {
                preventRedrawOnResize = true;
                graphics.SetFullScreen(!graphics.fullscreen, new Vector2i(settings.deferredShaderSize.x, settings.deferredShaderSize.y));
                preventRedrawOnResize = false;
            }
            lockF11 = toggleFullScreen;

            base.Update();

            if (state == State.Running)
                if (ChaosUtil.Platform.Windows.WinAPI.winuser.GetActiveWindow.Invoke() == window.Handle)
                    System.Windows.Forms.Cursor.Position = new System.Drawing.Point(window.Location.X + window.Width / 2, window.Location.Y + window.Height / 2);
        }

        protected override void Draw()
        {
            GL.ClearColor(new OpenTK.Graphics.Color4(0, (byte)Random.instance.RndInt(255), 0, 255));
            Graphics.ThrowErrors();
            GL.Clear(ClearBufferMask.ColorBufferBit);
            Graphics.ThrowErrors();
            base.Draw();
            graphics.graphicsContext.SwapBuffers();
        }

        void Terminate(object _, System.Windows.Forms.FormClosingEventArgs __)
            => Terminate();

        protected override void DoDispose()
        {
            window.FormClosing -= Terminate;
            base.DoDispose();
            textures?.Dispose();
            materials?.Dispose();
            meshes?.Dispose();
            fonts?.Dispose();
            graphics?.Dispose();
            audio.Dispose();
            samples.Dispose();
            music?.Dispose();
            shaders?.Dispose();
            shaderCode?.Dispose();
            animations?.Dispose();
            shapes?.Dispose();
        }
    }
}
