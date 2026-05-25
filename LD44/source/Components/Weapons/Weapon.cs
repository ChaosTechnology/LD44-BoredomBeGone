using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Collections;
using ChaosFramework.Collections.Immutable;
using ChaosFramework.Components;
using ChaosFramework.Physics;
using ChaosFramework.Physics.States;
using ChaosFramework.Shapes.Convex;
using ChaosUtil.Primitives;
using ChaosUtil.Reflection;
using System;
using SysCol = System.Collections.Generic;
using BindingFlags = System.Reflection.BindingFlags;

namespace LD44.Components.Weapons
{
    [AssemblyManager.ListSubTypes]
    [Weapon("Wooden Straight Sword", "Straight Sword", "Wooden Straight Sword", "Straight Sword", WeaponAttribute.SwipeDirection.Both, 5, 3, 15, "WoodenStraightSword")]
    [Weapon("Wooden Bat", "Bat", "Wooden Bat", "Bat", WeaponAttribute.SwipeDirection.Any, 10, 5, 20, "Bat")]
    [Weapon("Steel Straight Sword", "Straight Sword", "Steel Straight Sword", "Straight Sword", WeaponAttribute.SwipeDirection.Both, 15, 5, 50, "SteelStraightSword")]
    [Weapon("Steel War Hammer", "War Hammer", "Steel War Hammer", "War Hammer", WeaponAttribute.SwipeDirection.Any, 25, 25, 50, "SteelWarHammer")]
    [Weapon("Iron Bat", "Bat", "Iron Bat", "Bat", WeaponAttribute.SwipeDirection.Any, 25, 7.5f, 60, "Bat")]
    [Weapon("Dwarven War Hammer", "War Hammer", "Dwarven War Hammer", "War Hammer", WeaponAttribute.SwipeDirection.Any, 40, 25, 100, "SteelWarHammer")]
    [Weapon("Mace", "Mace", "Mace", "Mace", WeaponAttribute.SwipeDirection.Any, 50, 25, 500, "Mace")]
    [Weapon("Onyx Blade", "Straight Sword", "Onyx Blade", "Straight Sword", WeaponAttribute.SwipeDirection.Both, 65, 5, 1000, "SteelStraightSword")]
    [Weapon("Dawnstar", "Straight Sword", "Dawnstar", "Straight Sword", WeaponAttribute.SwipeDirection.Both, 80, 5f, 5000, "SteelStraightSword")]
    [Weapon("Morningstar", "Straight Sword", "Morningstar", "Straight Sword", WeaponAttribute.SwipeDirection.Both, 100, 6.66f, 666666, "SteelStraightSword", false)]
#if DEBUG
    [Weapon("Blade of Creation", "Straight Sword", "Morningstar", "Straight Sword", WeaponAttribute.SwipeDirection.Both, 666666, 5, 0, "SteelStraightSword", false)]
#endif
    public partial class Weapon : StrictComponent<Characters.StickMan>
    {
        public WeaponAttribute attr;
        protected ChaosFramework.Graphics.OpenGl.ChaosShader.Shader shader;
        protected MaterialContainer.Entry mat;
        protected MeshContainer.Entry mesh;
        public Physical physics;
        bool hasUpdatedOnce = false;

        protected override void Create(CreateParameters cparams)
        {
            CParams<string> args = cparams as CParams<string>;
            if (args == null)
                throw new ArgumentException("invalid argument");
            if (!WeaponAttribute.TryGetAttribute(args.v1, out attr))
                throw new ArgumentException("unknown weapon type");

            shader = parent.scene.game.shaders.Load("Shaders/Weapon.fx", this);
            mat = parent.scene.game.materials.Load(attr.material, this);
            mesh = parent.scene.game.meshes.Load(attr.mesh, this);
            physics = new Physical(this);
            physics.state = new RealPhysicsState();
            physics.shapes.Clear();
            physics.shapes.Add(parent.scene.game.shapes.Load(attr.physics, this).content);
            parent.scene.physics.Add(physics);

            physics.validateCollision = IsCollisionPartnerValid;
            physics.getRelevantShapes = GetRelevantShapes;
        }

        bool IsCollisionPartnerValid(Physical partner, CollisionData other)
            => partner != parent.physics && hasUpdatedOnce && !(partner.creator is Map.HeightMap);

        SysCol.IEnumerable<Shape> GetRelevantShapes(Physical partner)
            => IsCollisionPartnerValid(partner, null) ? physics.shapes : null;

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            if (!hasUpdatedOnce)
                scene.updateLayers[(int)WorldScene.UpdateLayers.PostCollision].Add(FirstUpdate);
        }

        void FirstUpdate()
            => hasUpdatedOnce = true;

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            scene.drawLayers[(int)WorldScene.DrawLayers.World].Add(DrawWorld);
            scene.drawLayers[(int)WorldScene.DrawLayers.Material].Add(DrawMaterial);
        }

        void DrawWorld()
            => Draw("World");

        void DrawMaterial()
            => Draw("Material");

        void Draw(string pass)
        {
            parent.scene.view.SetValues(shader, physics.state.GetTransform());
            shader.SetValue("environmentSampler", parent.scene.skybox.skyReflection);
            mat.content.SetValues(shader);
            mesh.content.Draw(shader, pass);
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            parent.scene.physics.Remove(physics);
        }
    }
}
