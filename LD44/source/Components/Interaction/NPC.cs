using ChaosFramework.Components;
using ChaosFramework.Math.Vectors;
using LD44.Components.Characters;

namespace LD44.Components.Interaction
{
    public class NPC : InteractionPoint
    {
        public StickMan littleMan;
        public bool hostile = false;
        public float aggroAt = -1;
        public float NPC_offset = -5;

        public override bool enabled => base.enabled && !hostile && littleMan != null && littleMan.alive && littleMan.health > 0;

        protected override void Create(CreateParameters cparams)
        {
            base.Create(cparams);
            GimmeLittleMan();
        }

        protected virtual void GimmeLittleMan()
        {
            littleMan = scene.AddComponent<StickMan>();
            littleMan.mat = scene.game.materials.Load("Materials/Characters/NPC.mat", this);
            littleMan.physics.isStatic = true;
            littleMan.onDeath.Add(Disable);
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            scene.updateLayers[(int)WorldScene.UpdateLayers.UpdateNpcAggro].Add(ConsiderWar);
            scene.updateLayers[(int)WorldScene.UpdateLayers.UpdateNpcAggro].Add(ConsiderSuicide);
            scene.updateLayers[(int)WorldScene.UpdateLayers.PostCollision].Add(MoveLittleMan);
        }

        void ConsiderWar()
        {
            if (!hostile && littleMan.alive && littleMan.health <= aggroAt)
            {
                littleMan.physics.isStatic = !(hostile = true);
                GetAngry();
            }
        }

        void ConsiderSuicide()
        {
            if (littleMan == null || !littleMan.alive || littleMan.health <= 0)
            {
                Disable();
                if ((pentagram.angle -= pentagram.angle * ftime) <= 0.01f)
                {
                    Dispose();
                    return;
                }
            }
        }

        protected virtual void MoveLittleMan()
        {
            if (!hostile)
            {
                float y = littleMan.physics.Support(new Vector3f(0, -1, 0)).y - littleMan.physics.state.position.y;
                littleMan.physics.state.position = p.state.position - new Vector3f(0, y - NPC_offset, 0);
                littleMan.physics.state.velocity = Vector3f.EMPTY;
            }
        }

        protected virtual void GetAngry()
            => Disable();

        protected override void _EndInteraction() { }
    }
}
