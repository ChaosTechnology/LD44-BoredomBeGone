using ChaosFramework.Collections;
using ChaosFramework.Components;
using ChaosFramework.Core;
using ChaosFramework.Graphics;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Graphics.OpenGl.Lights;
using ChaosFramework.Graphics.OpenGl.Lights.Intrinsic;
using ChaosFramework.Graphics.OpenGl.PostProcessors;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Platform;
using LD44.Components.Characters.Brains;
using LD44.Components.Interaction;
using OpenTK.Graphics.OpenGL;
using static ChaosFramework.Math.Constants;
using Interactions = LD44.Components.Interaction.Actions;

namespace LD44
{
    public class WorldScene : HudScene
    {
        public enum UpdateLayers
        {
            UpdateNpcAggro,
            PrepareUpdate,
            Input,
            Move,
            Collision,
            PostCollision,
            SpawnParticles,
            UpdateParticles,
            UpdateRenderTransforms,
            UpdateCamera,
            PostCollisionImmutable
        }

        public const float SATANS_SAFESPACE = 10;
        public const float CHOSEN_ONE_SPAWN_X = 0;
        public const float CHOSEN_ONE_SPAWN_Z = 2;

        protected AntiAliasing antiEdger = new AntiAliasing();
        public TransparencyRenderer transparencyRenderer;

        public Camera view;
        public readonly DeferredShader shader;
        public readonly LightSet lights;
        public ChaosFramework.Physics.PhysicsWorld physics;
        public Transparents.ParticleFire fire;
        public Components.LifeOrbs lifeorbs;
        Light sun;

        public TextureContainer.Entry pentagram;
        public Satan satan;
        public ChosenOne player;
        public Components.Map.HeightMap map;
        public Components.Skybox skybox;

        string[,] demonTexts = new[,] {
             {"Lilith", "Another mortal trying to kill me?"},
             {"Belial", "Burn in hell!"},
             {"Azazel"," Your time has come, mortal!" },
             {"Belphegor", "Your soul shall be mine!"},
             {"Asmodeus", "You dare to challenge me?"}
        };
        System.Type[] demonSpells = new[]
        {
            typeof(Components.Weapons.Spells.FireOrb),
            typeof(Components.Weapons.Spells.FireOrb),
            typeof(Components.Weapons.Spells.FlameOfHanz),
            typeof(Components.Weapons.Spells.FlameOfHanz),
            typeof(Components.Weapons.Spells.Lifedrain)
        };

        public WorldScene(Game game)
            : base(game, typeof(UpdateLayers), typeof(DrawLayers))
        {
            pentagram = game.textures.Load("Textures/Pentagram.png", this);

            view = new Camera();
            view.Update(
                new Vector3f(0, 0, -3),
                new Vector3f(0, 0, 1),
                new Vector3f(0, 1, 0),
                float.NaN,
                float.NaN,
                PI_QUART / 2,
                screenRatio: game.window.Ratio()
                );

            using (new AccessScope<Game>(game))
            {
                shader = new DeferredShader(
                    base.game.graphics,
                    view,
                    new Vector2i(
                        (game.settings.deferredShaderSize.x <= 0) ? game.window.width : game.settings.deferredShaderSize.x,
                        (game.settings.deferredShaderSize.y <= 0) ? game.window.height : game.settings.deferredShaderSize.y
                        ),
                    lights = new LightSet(),
                    new DeferredShaderIntrinsicLights[] { new DirectionalLightIntrinsics(1) },
                    new LightInstancerBase[] {
                        new PointLightInstancer(game.graphics, 128),
                        new PentagramInstancer(game.graphics, 7, pentagram),
                        }
                    );
            }

            transparencyRenderer = new TransparencyRenderer(
                base.game.graphics,
                (int)(shader.width * base.game.settings.transparencyScaling),
                (int)(shader.height * base.game.settings.transparencyScaling),
                shader.depthBuffer,
                game.settings.transparencyLayers
                );
            antiEdger = new ChaosFramework.Graphics.OpenGl.PostProcessors.AntiAliasing();

            transparencyRenderer.transparents.Add(fire = AddComponent<Transparents.ParticleFire>());
            transparencyRenderer.transparents.Add(lifeorbs = AddComponent<Components.LifeOrbs>());

            sun = new DirectionalLight(new Vector3f(1, -5, 1), new Rgba(1, 1, 1, 1));
            lights.Add(sun);

            skybox = AddComponent<Components.Skybox>();

            physics = AddComponent<ChaosFramework.Physics.PhysicsWorld>(CreateParameters.Create((int)UpdateLayers.Collision));
            physics.forces.Add(new ChaosFramework.Physics.Forces.LinearForce(new Vector3f(0, -9.81f, 0)));
            map = AddComponent<Components.Map.HeightMap>();

            satan = AddComponent<Satan>(new CParams<System.Tuple<float, float>>(new System.Tuple<float, float>(5, PI)));
            satan.position = new Vector2f(0, 5);

            AddComponent<Components.Map.Trees>();
            AddComponent<Components.Map.Hostility>();

            player = new ChosenOne();
            player.TakeControl(AddComponent<Components.Characters.StickMan>());
            player.physics.state.position = new Vector3f(
                CHOSEN_ONE_SPAWN_X,
                map.GetHeightAt(CHOSEN_ONE_SPAWN_X, CHOSEN_ONE_SPAWN_Z) + player.myMan.originHeight,
                CHOSEN_ONE_SPAWN_Z
                );
            view.Update(player.myMan.headPos + new Vector3f(0, 1, 0), view.Direction, view.Up);

            AddComponent<Components.Map.Walls>();

            satan.actions = new[]{
                new Interactions.NPCInteraction [] {
                    new Interactions.CameraDrive(),
                    new Interactions.RaiseNPC(),
                    new Interactions.Dialog(this, new [] {
                        new Interactions.Dialog.Line(this, "Satan", "Press F to pay me some respect!"),
                        new Interactions.Dialog.Line(this, "You", "HAIL SATAN!"),
                        new Interactions.Dialog.Line(this, "Satan", "Why did you summon me, mortal?"),
                        new Interactions.Dialog.Line(this, "You", "Please, I need a plot for my ludum dare entry."),
                        new Interactions.Dialog.Line(this, "Satan", "So be it. Bring me life energy and I will grant you great power."),
                        new Interactions.Dialog.Line(this, "You", "Noice!")
                    }),
                    new Interactions.Shop(),
                },
                new Interactions.NPCInteraction [] {
                    new Interactions.CameraDrive(),
                    new Interactions.Dialog(this, new [] {
                        new Interactions.Dialog.Line(this, "Satan", "You seek power?")
                    }),
                    new Interactions.Shop(),
                }
            };
            satan.littleMan.stats = new Components.Characters.PlayerStats(satan.littleMan, 1, 3.5f, 3.5f, 3.5f);

            NPC samuel = AddComponent<NPC>(CreateParameters.Create(new System.Tuple<float, float>(2.5f, PI)));
            samuel.littleMan.stats.agility = 1.5f;
            samuel.littleMan.stats.strength = 3f;
            samuel.littleMan.stats.constitution = 3f;
            samuel.littleMan.stats.magic = 3f;
            samuel.actions = new[] {
                new Interactions.NPCInteraction[] {
                    new Interactions.CameraDrive(),
                    new Interactions.RaiseNPC(),
                    new Interactions.Dialog(this, new[] {
                        new Interactions.Dialog.Line(this, "Samuel", "Yo motherfucker!"),
                        new Interactions.Dialog.Line(this, "Samuel", "I need you to do something for me, will ya?"),
                        new Interactions.Dialog.Line(this, "Samuel", "See those fucking demons right next to that horned motherfucker over there?"),
                        new Interactions.Dialog.Line(this, "Samuel", "Kill them for me and bring me their fucking heads.")
                    }),
                    new Interactions.Custom(() =>
                    {
                        const int NUM_DEMONS = 5;
                        const int DEMON_HEALTH = 1000;
                        LinkedList<Satan> demons = new LinkedList<Satan>();
                        System.Action<Satan> killedDemon = demon =>
                        {
                            demons.Remove(demon);
                            if(demons.empty)
                            {
                                foreach(Interactions.NPCInteraction[] phase in samuel.actions)
                                    foreach(Interactions.NPCInteraction action in phase)
                                        action.Dispose();

                                samuel.actions = new []
                                {
                                    new Interactions.NPCInteraction[]
                                    {
                                        new Interactions.CameraDrive(),
                                        new Interactions.Dialog(this, new[] {
                                            new Interactions.Dialog.Line(this, "Samuel", "Daaamn, you're one badass motherfucker."),
                                            new Interactions.Dialog.Line(this, "Samuel", "Lets go and teach that fucker satan a lesson."),
                                            new Interactions.Dialog.Line(this, "Samuel", "That son of a bitch didn't pay his last betting debt.")
                                        }),
                                        new Interactions.Custom(() =>
                                        {
                                            new Ally().TakeControl(samuel.littleMan);
                                            samuel.aggroAt = samuel.littleMan.health;
                                        })
                                    }
                                };
                            }
                        };

                        for (int i = 0; i < NUM_DEMONS; i++)
                        {
                            Satan foeOfSamuel = AddComponent<Satan>(CreateParameters.Create(new System.Tuple<float, float>(2.5f, PI)));
                            demons.Add(foeOfSamuel);
                            foeOfSamuel.aggroAt = float.MinValue;
                            foeOfSamuel.intendedOffHand = demonSpells[0];
                            foeOfSamuel.littleMan.SetHealth(DEMON_HEALTH);
                            foeOfSamuel.littleMan.stats = new Components.Characters.PlayerStats(foeOfSamuel.littleMan, 1f, 2, 2, 2);
                            foeOfSamuel.mesh = game.meshes.Load("Models/Fat Horns.gmdl", this);
                            foeOfSamuel.actions = new[]
                            {
                                new Interactions.NPCInteraction[] {
                                    new Interactions.Custom(() =>
                                    {
                                        foreach(Satan demon in demons)
                                            if(demon.NPC_offset != 0 && demon != foeOfSamuel)
                                            {
                                                demon.littleMan.SetHealth(demon.littleMan.health + DEMON_HEALTH);
                                                int demonLevel = (int)(demon.littleMan.health / DEMON_HEALTH) - 1;
                                                demon.intendedOffHand = demonSpells[demonLevel];
                                                demon.littleMan.stats.strength += 0.2f;
                                                demon.littleMan.stats.constitution += 0.2f;
                                                demon.littleMan.stats.magic += 0.2f;
                                            }
                                    }),
                                    new Interactions.CameraDrive(),
                                    new Interactions.RaiseNPC(),
                                    new Interactions.Dialog(this, new [] {
                                        new Interactions.Dialog.Line(this, demonTexts[(int)i, 0], demonTexts[(int)i, 1])
                                    }),
                                    new Interactions.Custom(() =>
                                    {
                                        foeOfSamuel.aggroAt = float.MaxValue;
                                    })
                                },
                                new Interactions.NPCInteraction[] { null }
                            };
                            float angle = (PI_2 * i) / NUM_DEMONS;
                            foeOfSamuel.position = 29 * new Vector2f((float)System.Math.Cos(angle), (float)System.Math.Sin(angle));
                            foeOfSamuel.littleMan.onDeath.Add(() => killedDemon(foeOfSamuel));
                        }
                    }),
                },
                new Interactions.NPCInteraction[] {
                    new Interactions.CameraDrive(),
                    new Interactions.Dialog(this, new[] { new Interactions.Dialog.Line(this, "Samuel", "Motherfucker?") }),
                }
            };
            samuel.littleMan.physics.state.baseTransform = Matrix.Scaling(1.5f);
            samuel.position = new Vector2f(39.95244f, -101.4329f);
            samuel.littleMan.mat = game.materials.Load("Materials/Characters/Samuel.mat", this);
            samuel.littleMan.mainHandWeapon = samuel.littleMan.AddComponent<Components.Weapons.Weapon>(new CParams<string>("Dawnstar"));
            samuel.littleMan.UpdateAnimations();
            samuel.littleMan.SetHealth(5000);

            satan.StartInteraction(player);

            AddComponent<Components.DeathScreen>();
        }

        void UpdateView()
            => view.Update(view.Position, view.Direction, view.Up, screenRatio: game.window.Ratio());

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            drawLayers[(int)DrawLayers.UpdateView].Add(UpdateView);
            drawLayers[(int)DrawLayers.BeginWorld].Add(shader.BeginWorld);
            drawLayers[(int)DrawLayers.BeginMaterial].Add(shader.BeginMaterial);
            drawLayers[(int)DrawLayers.RenderDeferredShader].Add(shader.Render);
            drawLayers[(int)DrawLayers.PrepareSky].Add(PrepareSky);
            drawLayers[(int)DrawLayers.Transparents].Add(DrawTransparents);
            drawLayers[(int)DrawLayers.Postprocessing].Add(Present);
        }

        void PrepareSky()
        {
            GL.Viewport(0, 0, game.window.width, game.window.height);
            Graphics.ThrowErrors();
            game.graphics.stateTracker.BindFramebuffer(FramebufferTarget.Framebuffer, null);
        }

        void DrawTransparents()
            => transparencyRenderer.Render(shader);

        void Present()
        {
            GL.Viewport(0, 0, game.window.width, game.window.height);
            Graphics.ThrowErrors();
            game.graphics.stateTracker.BindFramebuffer(FramebufferTarget.Framebuffer, null);
            antiEdger.normalFactor = 6f;
            antiEdger.positionFactor = .6f;
            antiEdger.Apply(shader);
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            shader?.Dispose();
            sun.Dispose();
            transparencyRenderer?.Dispose();
        }
    }
}
