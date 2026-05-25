using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math.Vectors;
using ChaosUtil.Primitives;
using static ChaosFramework.Math.Clamping;
using static ChaosFramework.Math.Constants;

namespace LD44.Components.Interaction.Actions
{
    class RaiseNPC : NPCInteraction
    {
        const float PARTICLE_INTERVAL_SPIRAL = 0.015f;
        const float PARTICLE_INTERVAL_FLOOR = 0.00666f;

        float underground;
        NPC npc;

        public float actionTime = 2f;
        float progress = 0;

        float spiralParticleTimer = 0;
        float floorParticleTimer = 0;
        float rad = Random.instance.Rnd(PI_2);

        protected override void StartInteraction()
        {
            base.StartInteraction();
            npc = interactPoint as NPC;
            if (interactPoint == null)
            {
                interactPoint.Conclude();
                return;
            }

            underground = npc.NPC_offset;
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            interactPoint.scene.updateLayers[(int)WorldScene.UpdateLayers.PostCollisionImmutable].Add(Arise);
            interactPoint.scene.updateLayers[(int)WorldScene.UpdateLayers.SpawnParticles].Add(Burn);
        }

        void Arise()
        {
            npc.NPC_offset = underground * Max(0, 1 - progress / actionTime);
            if ((progress += interactPoint.ftime) >= actionTime)
            {
                npc.NPC_offset = 0;
                interactPoint.Conclude();
            }
        }

        void Burn()
        {
            spiralParticleTimer -= interactPoint.ftime;
            while (spiralParticleTimer < 0)
            {
                spiralParticleTimer += PARTICLE_INTERVAL_SPIRAL;
                rad += 0.2f;
                SpawnParticle(Vector2f.Normalize(new Vector2f((float)System.Math.Cos(rad), (float)System.Math.Sin(rad))));
                SpawnParticle(0.9f * Vector2f.Normalize(new Vector2f((float)System.Math.Cos(-rad), (float)System.Math.Sin(-rad))));
            }

            floorParticleTimer -= interactPoint.ftime;
            while (floorParticleTimer < 0)
            {
                floorParticleTimer += PARTICLE_INTERVAL_FLOOR;
                float rad = Random.instance.Rnd(PI_2);
                SpawnParticle(Vector2f.Normalize(new Vector2f((float)System.Math.Cos(rad), (float)System.Math.Sin(rad))), 0.5f, 1f);
            }
        }

        void SpawnParticle(Vector2f pos, float size = 0.3f, float lifeTime = 2.5f)
        {
            float x = pos.x + interactPoint.position.x;
            float z = pos.y + interactPoint.position.y;
            scene.fire.particles.Add(new Transparents.ParticleFire.FireParticle(
                interactPoint.ftime,
                null,
                new Vector3f(x, scene.map.GetHeightAt(x, z), z),
                lifeTime,
                size,
                size,
                new Vector3f(0, 1, 0),
                new Rgba(1, 0.45f, 0.1f, 1)
                ));
        }
    }
}
