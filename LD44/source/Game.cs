using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Components;
using ChaosFramework.IO.Streams;
using ChaosFramework.IO.Streams.Sources;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Physics;
using ChaosFramework.Platform;
using ChaosFramework.Sound;
using ChaosFramework.Sound.OpenAL;
using ChaosUtil.Primitives;
using ChaosUtil.Serialization.Text;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace LD44
{
    public class Game : BaseGame
    {
        public class LD44Mouse
        {
            public bool rightButton, leftButton;
            public float X, Y, WheelPrecise;
        }

        public class LD44Keyboard
        {
            public Dictionary<OpenTK.Windowing.GraphicsLibraryFramework.Keys, bool> isDown = new();
            public bool IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys keys)
            {
                return isDown.GetValueOrDefault(keys, false);
            }
        }

        public static readonly LD44Mouse _mouse = new LD44Mouse();
        public static readonly LD44Keyboard _keyboard = new LD44Keyboard();

        public readonly LD44Mouse mouse = _mouse;
        public readonly LD44Keyboard keyboard = _keyboard;

        public StreamSource assetSource {get; private set; }

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
        public Window window;

        Music music;

        bool preventRedrawOnResize = false;

        public readonly PlatformContext platformContext;

        bool lockF11;

        public Game(PlatformContext platformContext, Window window)
            : base((MessageQueue)platformContext) // TODO: merge MessageQueue interface into PlatformContext
        {
            this.window = window;
            this.platformContext = platformContext;
            platformContext.Terminate += Terminate;
        }

        public override void LoadGame()
        {
            base.LoadGame();
            gameLoop = new ChaosFramework.Components.GameLoop.CappedVariableTimeLoop((MessageQueue)platformContext, settings.maxFPS);

            assetSource = new ChaosFramework.IO.ChaosArchive(new System.IO.FileInfo("./assets.cha"), false);

            audio = new Audio();
            samples = new SoundDataContainer(assetSource, false);
            music = new Music(audio, assetSource.OpenRead("Music/music.ogg"));
            music.PlayLoop(1);

            graphics = new Graphics(platformContext.glContext, 3, 3);
            (fonts = new FontContainer(assetSource, graphics, false)).LoadDirectory("Fonts", new[] { ".chf2" }, true, this);
            (textures = new TextureContainer(assetSource, graphics.dispatcher, false)).LoadDirectory("Textures", new[] { ".png" }, true, this);
            (materials = new MaterialContainer(assetSource, graphics, textures, false)).LoadDirectory("Materials", new[] { ".mat" }, true, this);
            (meshes = new MeshContainer(assetSource, graphics.dispatcher, false)).LoadDirectory("Models", new[] { ".gmdl" }, true, this);
            (shaderCode = new ShaderCodeContainer(new StreamSourceCollection(StreamSources.shaderCode, assetSource))).LoadDirectory("Shaders", new[] { ".fx" }, true, this);
            (shaders = new ShaderContainer(assetSource, graphics, shaderCode)).LoadDirectory("Shaders", new[] { ".fx" }, true, this);
            (animations = new AnimationContainer(assetSource, false)).LoadDirectory("Animations", new[] { ".anim" }, true, this);
            (shapes = new ShapeContainer(assetSource)).LoadDirectory("Models", new[] { ".obj" }, true, this);
            scenes.Add(new WorldScene(this));

            // window.BackgroundImage.Dispose();
            // window.BackgroundImage = null;
        }

        protected override void Update()
        {
            bool toggleFullScreen = keyboard.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.F11);
            if (toggleFullScreen && !lockF11)
            {
                preventRedrawOnResize = true;
                // graphics.SetFullScreen(!graphics.fullscreen, new Vector2i(settings.deferredShaderSize.x, settings.deferredShaderSize.y));
                preventRedrawOnResize = false;
            }
            lockF11 = toggleFullScreen;

            base.Update();

            // if (state == State.Running)
            //     if (ChaosUtil.Platform.Windows.WinAPI.winuser.GetActiveWindow.Invoke() == window.Handle)
            //         System.Windows.Forms.Cursor.Position = new System.Drawing.Point(window.Location.X + window.Width / 2, window.Location.Y + window.Height / 2);
        }

        protected override void Draw()
        {
            GL.ClearColor(0, (byte)ChaosUtil.Primitives.Random.instance.RndInt(255), 0, 255);
            Graphics.ThrowErrors();
            GL.Clear(ClearBufferMask.ColorBufferBit);
            Graphics.ThrowErrors();
            base.Draw();
            window.Present();
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            platformContext.Terminate -= Terminate;
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
