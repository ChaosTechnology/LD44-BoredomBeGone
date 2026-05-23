using ChaosFramework.Math.Vectors;

namespace LD44.Components.Interaction.Actions
{
    class CameraDrive : NPCInteraction
    {
        Vector3f camTargetPos, camTargetDir;

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            interactPoint.scene.updateLayers[(int)WorldScene.UpdateLayers.PostCollision].Add(UpdateCamera);
        }

        void UpdateCamera()
        {
            Vector3f targetPos = new Vector3f(interactPoint.position.x, player.myMan.headPos.y, interactPoint.position.y);
            NPC npc = interactPoint as NPC;
            if (npc != null)
                targetPos = npc.littleMan.headPos - new Vector3f(0, npc.NPC_offset, 0);

            Vector3f avgHead = (player.myMan.headPos + targetPos) * 0.5f;
            camTargetPos = avgHead + -2 * Vector3f.Cross(player.myMan.headPos - targetPos, scene.view.Up);
            camTargetDir = avgHead - camTargetPos;
            Vector3f pos = scene.view.Position + (camTargetPos - scene.view.Position) * interactPoint.ftime * 2;
            Vector3f dir = scene.view.Direction + (camTargetDir - scene.view.Direction) * interactPoint.ftime * 2;
            scene.view.Update(pos, dir, scene.view.Up);

            if ((camTargetPos - pos).LengthSq() < 0.01f)
                interactPoint.Conclude();
        }
    }
}
