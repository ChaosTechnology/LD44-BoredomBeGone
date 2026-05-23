using ChaosFramework.Collections;
using ChaosFramework.Collections.Immutable;
using ChaosFramework.Components;
using ChaosFramework.Graphics;
using ChaosFramework.Math.Vectors;
using ChaosUtil.Primitives;
using ChaosUtil.Reflection;
using static ChaosFramework.Math.Exponentials;
using static ChaosFramework.Math.Signs;
using System.Linq;

namespace LD44.Components.Map
{
    using Weapons;

    public class Hostility : Component<WorldScene>
    {

        const int NUM_ENEMIES = 200;
        const float DIST_PHYSICS = 50 * 50;
        const float DIST_GRAPHICS = 50 * 50;

        AdvancedLinkedList<System.Tuple<Characters.StickMan, Wrapper<bool>>> enemyEnabled = [];

        protected override void Create(CreateParameters cparams)
        {
            using (System.Drawing.Bitmap chanceMap = new System.Drawing.Bitmap(Game.assetSource.OpenRead("textures/map/foeLoc.png")))
            using (System.Drawing.Bitmap statMap = new System.Drawing.Bitmap(Game.assetSource.OpenRead("textures/map/foeStat.png")))
            {
                byte[] chance = BitmapUtils.GetBitmapBytes(chanceMap);
                byte[] stats = BitmapUtils.GetBitmapBytes(statMap);
                int width = statMap.Width;
                int height = statMap.Height;

                int numSuccessfulSpawns = 0;
                while (numSuccessfulSpawns < NUM_ENEMIES)
                {
                    Vector2f rnd = new Vector2f(Random.instance.Rnd(1), Random.instance.Rnd(1));
                    int pixel = (int)(width * rnd.x) + width * (int)(rnd.y * height);

                    byte probability = chance[4 * pixel + 1];
                    if (Random.instance.RndByte() > probability)
                        continue;

                    Vector3f pos = new Vector3f(HeightMap.MAP_SIZE * (rnd.x - 0.5f), 0, HeightMap.MAP_SIZE * (0.5f - rnd.y));
                    if (Abs(pos.x - scene.satan.position.x) < WorldScene.SATANS_SAFESPACE ||
                        Abs(pos.z - scene.satan.position.y) < WorldScene.SATANS_SAFESPACE)
                        continue;

                    byte vit = chance[4 * pixel + 1];
                    byte str = chance[4 * pixel + 2];
                    byte dex = stats[4 * pixel + 0];
                    byte mag = stats[4 * pixel + 1];
                    byte con = stats[4 * pixel + 2];

                    numSuccessfulSpawns++;
                    Characters.StickMan villain = scene.AddComponent<Characters.StickMan>();
                    new Characters.Brains.Enemy().TakeControl(villain);
                    villain.SetHealth(10 + 490 * (vit / 255f) * (vit / 255f) * (vit / 255f));
                    villain.stats.strength = 1 + str / 255f;
                    villain.stats.agility = 1 + dex / 255f;
                    villain.stats.magic = 1 + mag / 255f;
                    villain.stats.constitution = 1 + con / 255f;

                    pos.y = scene.map.GetHeightAt(pos.x, pos.z);
                    pos.y += villain.originHeight + 1;
                    villain.physics.state.position = pos;

                    float spellInterval = (float)System.Math.Ceiling(256f / (OffHand.spells.length + 1));
                    int spellIndex = mag / (int)spellInterval;
                    if (spellIndex > 1)
                        villain.offHandWeapon = (Weapons.OffHand)villain.AddComponent(OffHand.spells[spellIndex - 1].type);

                    float wpnInterval = (float)System.Math.Ceiling(256f / (Weapon.WeaponAttribute.gruntNames.length - 1));
                    int wpnIndex = (int)(Sqrt(str / 255f) * 255) / (int)wpnInterval;
                    villain.mainHandWeapon = (Weapons.Weapon)villain.AddComponent(typeof(Weapons.Weapon), new CParams<string>(Weapon.WeaponAttribute.gruntNames[wpnIndex]));

                    enemyEnabled.Add(new System.Tuple<Characters.StickMan, Wrapper<bool>>(villain, new Wrapper<bool>(false)));
                    villain.mat = scene.game.materials.Load("Materials/Characters/Enemy.mat", this);
                    villain.UpdateAnimations();
                    villain.TogglePhysics();
                    villain.doDraw = false;
                }
            }
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            scene.updateLayers[(int)WorldScene.UpdateLayers.PostCollision].Add(UpdateEnemyEnable);
        }

        void UpdateEnemyEnable()
        {
            foreach (System.Tuple<Characters.StickMan, Wrapper<bool>> enemy in enemyEnabled)
            {
                if (!enemy.Item1.alive)
                {
                    enemyEnabled.RemoveCurrent();
                    continue;
                }

                float distSq = (enemy.Item1.physics.state.position - scene.player.physics.state.position).LengthSq();
                enemy.Item1.doDraw = distSq < DIST_GRAPHICS;

                bool phyRange = distSq < DIST_PHYSICS;
                if (phyRange && !enemy.Item2 || !phyRange && enemy.Item2)
                {
                    enemy.Item2.value = !enemy.Item2.value;
                    enemy.Item1.TogglePhysics();
                }
            }
        }
    }
}
