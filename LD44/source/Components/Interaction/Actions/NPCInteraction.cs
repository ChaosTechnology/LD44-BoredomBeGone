using ChaosFramework.Core;

namespace LD44.Components.Interaction.Actions
{
    public abstract class NPCInteraction : Disposable
    {
        public InteractionPoint interactPoint { get; private set; }
        public Characters.Brains.ChosenOne player { get; private set; }

        protected virtual WorldScene scene => interactPoint.scene;

        public void StartInteraction(InteractionPoint npc, Characters.Brains.ChosenOne player)
        {
            this.interactPoint = npc;
            this.player = player;
            StartInteraction();
        }

        protected virtual void StartInteraction() { }

        public virtual void SetUpdateCalls() { }
        public virtual void SetDrawCalls() { }
    }
}
