using System;

namespace LD44.Components.Interaction.Actions
{
    class Custom : NPCInteraction
    {
        readonly Action action;

        public Custom(Action action)
        {
            this.action = action;
        }

        protected override void StartInteraction()
        {
            base.StartInteraction();
            action?.Invoke();
            interactPoint.Conclude();
        }
    }
}
