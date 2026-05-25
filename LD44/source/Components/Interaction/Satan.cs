using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Components;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;

namespace LD44.Components.Interaction
{
    public class Satan : NPC
    {
        Transparents.ParticleFire.Spawner leftHand, rightHand;

        protected ChaosFramework.Graphics.OpenGl.ChaosShader.Shader shader;
        protected MaterialContainer.Entry mat;
        public MeshContainer.Entry mesh;
        Matrix spawnMatrix;
        float face = 0;

        public System.Type intendedOffHand = typeof(Weapons.Spells.FlameOfHanz);
        public string intendedWeapon = "Morningstar";

        protected override void Create(CreateParameters cparams)
        {
            face = (cparams as CParams<System.Tuple<float, float>>).v1.Item2;
            base.Create(cparams);
            leftHand = littleMan.AddComponent<Transparents.ParticleFire.Spawner>();
            rightHand = littleMan.AddComponent<Transparents.ParticleFire.Spawner>();
            shader = scene.game.graphics.shaders.normalMap;
            mat = scene.game.materials.Load("Materials/Characters/Horns.mat", this);
            mesh = scene.game.meshes.Load("Models/Horns.gmdl", this);
            aggroAt = 9900f;
            spawnMatrix = littleMan.physics.state.baseTransform;
        }

        protected override void GimmeLittleMan()
        {
            littleMan = scene.AddComponent<Characters.StickMan>();
            littleMan.physics.isStatic = true;
            littleMan.mat = scene.game.materials.Load("Materials/Characters/NPC.mat", this);
            littleMan.SetHealth(10000f);
            littleMan.stats.magic = 2.5f;
            littleMan.stats.strength = 2.5f;
            littleMan.stats.constitution = 2.5f;
            littleMan.mat = scene.game.materials.Load("Materials/Characters/Satan.mat", this);
            littleMan.physics.state.baseTransform = Matrix.Scaling(1.5f) * Matrix.RotationY(face);
        }

        protected override void GetAngry()
        {
            base.GetAngry();
            new Characters.Brains.Boss().TakeControl(littleMan);
            littleMan.offHandWeapon = (Weapons.OffHand)littleMan.AddComponent(intendedOffHand);
            littleMan.mainHandWeapon = littleMan.AddComponent<Weapons.Weapon>(CreateParameters.Create(intendedWeapon));
            leftHand.Dispose();
            rightHand.Dispose();
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            scene.updateLayers[(int)WorldScene.UpdateLayers.Move].Add(Move);
            scene.updateLayers[(int)WorldScene.UpdateLayers.PostCollision].Add(ConsiderPeace);
        }

        void Move()
        {
            Matrix t = littleMan.physics.state.GetTransform();
            rightHand.position = Vector3f.TransformCoordinate(new Vector3f(0, littleMan.boneSwordArm.length, 0), littleMan.boneSwordArm.restTransform * littleMan.boneSwordArm.transform * t);
            leftHand.position = Vector3f.TransformCoordinate(new Vector3f(0, littleMan.boneSpellArm.length, 0), littleMan.boneSpellArm.restTransform * littleMan.boneSpellArm.transform * t);
        }

        void ConsiderPeace()
        {
            if (!hostile)
                littleMan.physics.state.baseTransform = spawnMatrix;
        }

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            scene.drawLayers[(int)HudScene.DrawLayers.World].Add(DrawWorld);
            scene.drawLayers[(int)HudScene.DrawLayers.Material].Add(DrawMaterial);
        }

        void DrawWorld()
            => Draw("World");

        void DrawMaterial()
            => Draw("Material");

        void Draw(string pass)
        {
            if (littleMan != null && littleMan.alive && littleMan.health > 0)
            {
                Matrix transform = littleMan.physics.state.GetTransform();
                scene.view.SetValues(shader, littleMan.boneHead.restTransform * littleMan.boneHead.transform * transform);
                mat.content.SetValues(shader);
                mesh.content.Draw(shader, pass);
            }
        }
    }
}
