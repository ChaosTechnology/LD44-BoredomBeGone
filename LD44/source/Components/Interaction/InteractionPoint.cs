using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Components;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Physics;
using ChaosFramework.Shapes.Convex;
using ChaosUtil.Primitives;
using static ChaosFramework.Math.Clamping;
using static ChaosFramework.Math.Constants;
using SysCol = System.Collections.Generic;

namespace LD44.Components.Interaction
{
    public abstract class InteractionPoint : Component<WorldScene>
    {
        int currentPhase;
        int currentAction;
        Actions.NPCInteraction currentInteraction => actions[currentPhase][currentAction];

        Actions.NPCInteraction[][] _actions;
        public Actions.NPCInteraction[][] actions
        {
            get { return _actions; }
            set
            {
                _actions = value;
                currentPhase = currentAction = 0;
            }
        }

        public Vector2f position
        {
            set
            {
                p.state.position = new Vector3f(value.x, scene.map.GetHeightAt(value.x, value.y), value.y);
                pentagram.position = p.state.position + new Vector3f(0, radius, 0);
            }
            get { return p.state.position.xz; }
        }

        float radius = 1.5f;
        float rotation = Random.instance.Rnd(PI_2);

        protected Physical p;
        protected Characters.Brains.ChosenOne interactor;

        public virtual bool enabled { get; private set; } = true;
        public void Disable() => enabled = false;

        protected ChaosFramework.Graphics.OpenGl.Lights.SpotLight pentagram;

        protected override void Create(CreateParameters cparams)
        {
            CParams<System.Tuple<float, float>> a = cparams as CParams<System.Tuple<float, float>>;

            p = new Physical(this, false);
            p.shapes.Clear();
            p.shapes.Add(new SphereShape(Vector3f.EMPTY, 1));

            p.validateCollision = IsCollisionPartnerValid;
            p.getRelevantShapes = GetRelevantShapes;

            p.state.baseTransform = Matrix.Scaling(radius = a.v1.Item1) * Matrix.RotationY(a.v1.Item2);
            p.isStatic = true;
            scene.physics.Add(p);

            pentagram = new ChaosFramework.Graphics.OpenGl.Lights.MaskedSpotLight(
                p.state.position + new Vector3f(0, radius, 0),
                new Rgba(10, 10, 10, 1),
                radius * 2,
                PI_HALF / 2,
                scene.pentagram
                );
            pentagram.direction = new Vector3f(0, -1, 0);
            pentagram.up = new Vector3f(0, 0, 1);
            scene.lights.Add(pentagram);
        }

        bool IsCollisionPartnerValid(Physical partner, CollisionData _)
            => !Characters.Brains.ChosenOne.GimmeMyChosenOne(partner)?.interacting ?? false;

        SysCol.IEnumerable<Shape> GetRelevantShapes(Physical s)
            => IsCollisionPartnerValid(s, null) ? p.shapes : null;

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            if (interactor != null)
                currentInteraction.SetUpdateCalls();

            scene.updateLayers[(int)WorldScene.UpdateLayers.Move].Add(Move);
            scene.updateLayers[(int)WorldScene.UpdateLayers.PostCollision].Add(Interact);
        }

        void Move()
            => p.state.Move(ftime);

        void Interact()
        {
            if (enabled)
                foreach (CollisionData col in p.currentCollisions)
                {
                    Characters.Brains.ChosenOne collider = Characters.Brains.ChosenOne.GimmeMyChosenOne(col);
                    if (collider != null)
                        if (!collider.interacting && collider.triesToInteract)
                            StartInteraction(collider);
                }
        }

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            if (interactor != null)
                currentInteraction.SetDrawCalls();
        }

        public void Conclude()
        {
            if ((currentAction + 1) >= actions[currentPhase].Length)
                EndInteraction();
            else
            {
                currentAction++;
                currentInteraction.StartInteraction(this, interactor);
            }
        }

        public void StartInteraction(Characters.Brains.ChosenOne player)
        {
            if (currentInteraction == null)
                return;

            player.interacting = true;
            player.triesToInteract = false;
            interactor = player;
            interactor.physics.isStatic = true;
            interactor.physics.state.velocity = Vector3f.EMPTY;
            currentAction = 0;
            currentInteraction.StartInteraction(this, player);
        }

        protected abstract void _EndInteraction();
        protected void EndInteraction()
        {
            currentPhase = Min(actions.Length - 1, currentPhase + 1);
            currentAction = 0;
            interactor.interacting = false;
            interactor.physics.isStatic = false;
            _EndInteraction();
            interactor = null;
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            foreach (Actions.NPCInteraction[] phase in actions)
                foreach (Actions.NPCInteraction action in phase)
                    action?.Dispose();

            pentagram.Dispose();
        }
    }
}
