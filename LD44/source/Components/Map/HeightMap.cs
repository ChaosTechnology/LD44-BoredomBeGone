using ChaosFramework.Shapes.Convex;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.Model;
using ChaosFramework.Collections;
using ChaosFramework.Components;
using ChaosFramework.Graphics.OpenGl.ChaosShader;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Physics;
using OpenTK.Graphics.OpenGL;
using System.Linq;
using System.IO;
using ChaosFramework.Graphics.Imaging.Formats;
using ChaosFramework.Graphics.Imaging;
using System;

namespace LD44.Components.Map
{
    public class HeightMap : Component<WorldScene>
    {
        static readonly System.Text.RegularExpressions.Regex mapfileRegex = new System.Text.RegularExpressions.Regex(
            @"[\\/]*Materials[\\/]+Map[\\/]+\d\.mat",
            System.Text.RegularExpressions.RegexOptions.Compiled
            );

        public const float MAP_SIZE = 512;
        public const float mapHeight = 15;
        public const int matmapsz = 4096;

        static bool IsMapFile(string str)
            => mapfileRegex.IsMatch(str);

        // TODO: Lazy<Programmer> detected
        static System.Lazy<Func<RawDataHandle>> blackFallback = new System.Lazy<Func<RawDataHandle>>(() =>
            Rgba8Image.CreateEmpty(matmapsz, matmapsz).GetRawData
            );

        static Func<RawDataHandle> SelectOrFallback(Func<RawDataHandle> x)
            => x ?? blackFallback.Value;

        static Vector3f EpaCellSupport(Vector3f searchDirection, MeshShape cellShape)
        {
            Vector3f result = cellShape.transformedVerts[0];
            float d = Vector3f.Dot(searchDirection, result);
            float newLength = 0;
            for (int i = 1; i < 4; i++)
                if ((newLength = Vector3f.Dot(cellShape.transformedVerts[i], searchDirection)) > d)
                {
                    d = newLength;
                    result = cellShape.transformedVerts[i];
                }
            return result;
        }

        HeightMapData mapData;
        MeshShape[,] cellShapes;

        TextureContainer.Entry mapmap;
        TextureArray textures;

        Mesh mesh;
        Shader shader;
        Physical physics;

        protected override void Create(CreateParameters cparams)
        {
            mapmap = scene.game.textures.Load("Textures/Map/MapMap.png", this);

            LinkedList<Func<RawDataHandle>[]> tex = new LinkedList<Func<RawDataHandle>[]>();
            foreach (string matKey in Game.assetSource.EnumerateKeys().Where(IsMapFile))
            {
                Func<RawDataHandle>[] matTex = new Func<RawDataHandle>[4];
                tex.Add(matTex);

                using (Stream matStr = Game.assetSource.OpenRead(matKey))
                using (StreamReader matRd = new StreamReader(matStr))
                {
                    Material.LayerKeys meta = Material.Parse(matRd, matKey);
                    matTex[0] = meta.normal == null ? null : Png.FromStream(Game.assetSource.OpenRead(meta.normal)).GetRawData;
                    matTex[1] = meta.emissive == null ? null : Png.FromStream(Game.assetSource.OpenRead(meta.emissive)).GetRawData;
                    matTex[2] = meta.diffuse == null ? null : Png.FromStream(Game.assetSource.OpenRead(meta.diffuse)).GetRawData;
                    matTex[3] = meta.specular == null ? null : Png.FromStream(Game.assetSource.OpenRead(meta.specular)).GetRawData;
                }
            }

            textures = new TextureArray(
                scene.game.graphics.dispatcher,
                new Texture.Parameters(
                    matmapsz,
                    matmapsz,
                    pixelType: PixelType.UnsignedByte,
                    pixelFormat: PixelFormat.Rgba,
                    internalFormat: PixelInternalFormat.Rgba8,
                    minFilter: TextureMinFilter.LinearMipmapLinear
                    ),
                tex.SelectMany(Linq.SelectIdentity).Select(SelectOrFallback).ToArray()
                );

            GL.BindTexture(TextureTarget.Texture2DArray, textures.textureIndex);
            Graphics.ThrowErrors();
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2DArray);
            Graphics.ThrowErrors();

            mapData = new HeightMapData(Game.assetSource.OpenRead("Textures/Map/Height.png"));
            physics = new Physical(this);
            physics.shapes.Clear();
            physics.isStatic = true;
            physics.state.position = new Vector3f(0, -mapHeight, 0);
            physics.state.baseTransform = Matrix.Scaling(MAP_SIZE, mapHeight, MAP_SIZE);
            physics.AddShape(new MeshShape(HeightMapData.hullBoxVerts));
            physics.getRelevantShapes = GetReleveantShapes;
            scene.physics.Add(physics);
            cellShapes = new MeshShape[mapData.vertsPerRow - 1, mapData.vertsPerRow - 1];
            Matrix transform = physics.state.GetTransform();
            for (int x = 0; x < mapData.vertsPerRow - 1; x++)
                for (int z = 0; z < mapData.vertsPerRow - 1; z++)
                {
                    MeshShape currentShape = cellShapes[x, z] = new MeshShape(mapData.physicalShapeVerts[x, z]);
                    currentShape.Update(transform, true);
                    currentShape.epaSupportFunction = searchDirection => EpaCellSupport(searchDirection, currentShape);
                }
            shader = scene.game.shaders.Load("Shaders/Map.fx", this);
            mesh = mapData.CreateMesh(scene.game.graphics);
        }

        LinkedList<Shape> GetReleveantShapes(Physical collisionPartner)
        {
            if (collisionPartner.isStatic)
                return null;

            LinkedList<Shape> output = new LinkedList<Shape>();
            Vector2f low = GetCellCoords(collisionPartner.boundingBoxLow);
            Vector2f high = GetCellCoords(collisionPartner.boundingBoxHigh);
            int lowX = System.Math.Max(1, (int)System.Math.Floor(low.x));
            int lowZ = System.Math.Max(1, (int)System.Math.Floor(low.y));
            int highX = System.Math.Min(mapData.vertsPerRow - 2, (int)System.Math.Ceiling(high.x));
            int highZ = System.Math.Min(mapData.vertsPerRow - 2, (int)System.Math.Ceiling(high.y));
            for (int x = lowX - 1; x <= highX; x++)
                for (int z = lowZ - 1; z <= highZ; z++)
                    if (cellShapes[x, z] != null)
                        output.Add(cellShapes[x, z]);

            return output;
        }

        Vector2f GetCellCoords(Vector3f absoluteWorldPosition)
        {
            Vector2f relativePos = (absoluteWorldPosition.xz - physics.state.position.xz) / MAP_SIZE;
            return (relativePos + 0.5f) * mapData.numCells;
        }

        public float GetHeightAt(float x, float z)
        {
            Vector2f cellCoord = GetCellCoords(new Vector3f(x, 0, z));
            if (cellCoord.x < 0 || cellCoord.x >= mapData.numCells || cellCoord.y < 0 || cellCoord.y >= mapData.numCells)
                return 0;

            float lxly = mapData.heightValues[(int)System.Math.Floor(cellCoord.x), (int)System.Math.Floor(cellCoord.y)];
            float lxhy = mapData.heightValues[(int)System.Math.Floor(cellCoord.x), (int)System.Math.Ceiling(cellCoord.y)];
            float hxly = mapData.heightValues[(int)System.Math.Ceiling(cellCoord.x), (int)System.Math.Floor(cellCoord.y)];
            float hxhy = mapData.heightValues[(int)System.Math.Ceiling(cellCoord.x), (int)System.Math.Ceiling(cellCoord.y)];
            float fx = cellCoord.x % 1, fy = cellCoord.y % 1;
            float ly = lxly + (hxly - lxly) * fx;
            float hy = lxhy + (hxhy - lxhy) * fx;
            float h = ly + (hy - ly) * fy;
            return Vector3f.TransformCoordinate(new Vector3f(0, h, 0), physics.state.GetTransform()).y;
        }

        public override void SetDrawCalls()
        {
            base.SetDrawCalls();
            scene.drawLayers[(int)WorldScene.DrawLayers.World].Add(DrawGroundWorld);
            scene.drawLayers[(int)WorldScene.DrawLayers.Material].Add(DrawGroundMaterial);
        }

        void DrawGroundWorld()
            => DrawGround("World");

        void DrawGroundMaterial()
            => DrawGround("Material");

        void DrawGround(string pass)
        {
            scene.shader.view.SetValues(shader, physics.state.GetTransform());
            shader.SetValue("maps", textures);
            shader.SetValue("mappingSampler", mapmap);
            mesh.Draw(shader, pass);
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            mesh.Dispose();
            textures.Dispose();
        }
    }
}
