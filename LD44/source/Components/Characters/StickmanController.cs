using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math.Vectors;

namespace LD44.Components.Characters
{
    public abstract class StickmanController
    {
        public StickMan myMan { get; private set; }

        public WorldScene scene => myMan.scene;
        public ChaosFramework.Physics.Physical physics => myMan.physics;
        public ChaosFramework.Components.Time ftime => myMan.ftime;

        public void TakeControl(StickMan myMan)
        {
            this.myMan = myMan;
            if (myMan.brain != this)
                myMan.brain?.DropControl();

            myMan.brain = this;
            TakeControl();
        }

        public virtual void SetUpdateCalls()
            => scene.updateLayers[(int)WorldScene.UpdateLayers.PostCollision].Add(StickToMap);

        void StickToMap()
        {
            if (myMan != null)
            {
                Vector3f myCore = physics.state.position;
                float feet = physics.boundingBoxLow.y - myCore.y;
                float ground = scene.map.GetHeightAt(myCore.x, myCore.z);
                float underMap = myCore.y + feet - ground;

                if (underMap < 0)
                {
                    physics.state.position.y = ground - feet;
                    physics.state.velocity.y = 0;
                    myMan.lastGrounded = 0;
                }
            }
        }

        public virtual void CreateDamageDisplay(float displayDamage)
        {
            EffectText textDisplay = scene.AddComponent<EffectText>();
            textDisplay.text = "-" + displayDamage;
            textDisplay.color = new Rgba(1, 0, 0, 1);
            textDisplay.position = myMan.headPos + new Vector3f(0.15f, 0.5f, 0);
        }

        public virtual void Move() { }
        public virtual void Input() { }
        public abstract void DrawHUD();
        public virtual void TakeControl() { }
        public virtual void DropControl() => myMan = null;
        public virtual bool TryingToCast() => false;
        public virtual void Die() { }
    }
}
