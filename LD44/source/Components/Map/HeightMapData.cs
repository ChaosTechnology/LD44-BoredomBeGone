using ChaosFramework.Collections;
using ChaosFramework.Graphics.OpenGl;
using ChaosFramework.Graphics.OpenGl.Model;
using ChaosFramework.Math.Vectors;
using System;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SysCol = System.Collections.Generic;
using SysDraw = System.Drawing;

namespace LD44.Components.Map
{
    internal class HeightMapData
    {
        const float LOW_CUT = -25;
        const float ROUGHNESS = 3;
        const int NUM_TEX_REPEATS = 1;

        static SysCol.Dictionary<Graphics, Mesh> graphicsMeshes = new SysCol.Dictionary<Graphics, Mesh>();
        public static HeightMapData[] presets;

        public static Vector3f[] hullBoxVerts = new Vector3f[] {
            new Vector3f(-0.5f, LOW_CUT, -0.5f),
            new Vector3f(-0.5f, LOW_CUT, 0.5f),
            new Vector3f(-0.5f, ROUGHNESS, -0.5f),
            new Vector3f(-0.5f, ROUGHNESS, 0.5f),
            new Vector3f( 0.5f, LOW_CUT, -0.5f),
            new Vector3f( 0.5f, LOW_CUT, 0.5f),
            new Vector3f( 0.5f, ROUGHNESS, -0.5f),
            new Vector3f( 0.5f, ROUGHNESS, 0.5f),
        };

        static readonly object staticCtorLock = new object();
        static HeightMapData()
        {
            lock (staticCtorLock)
            {
                string path;
                int i = 0;
                LinkedList<HeightMapData> data = new LinkedList<HeightMapData>();
                while (System.IO.File.Exists(path = "Tiles/HeightMaps/" + i++ + ".png"))
                    data.Add(new HeightMapData(Game.assetSource.OpenRead(path)));
                presets = data.ToArray();
            }
        }

        public float cellSize, numCells;
        public int vertsPerRow;
        public float[,] heightValues;

        Vector3f[] positions, normals;
        Vector4f[] tangents;
        Vector2f[] texCoords;
        uint[] indices;

        public Vector3f[,][] physicalShapeVerts;

        public HeightMapData(System.IO.Stream srcFile)
        {
            SysDraw.Bitmap img = new SysDraw.Bitmap(srcFile);
            vertsPerRow = img.Width;
            if (vertsPerRow != img.Height)
                throw new Exception("map must be square");

            BitmapData bitMap = img.LockBits(new SysDraw.Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] data = new byte[bitMap.Stride * bitMap.Height];
            heightValues = new float[vertsPerRow, vertsPerRow];
            try
            {
                Marshal.Copy(bitMap.Scan0, data, 0, data.Length);
                for (int x = 0; x < bitMap.Width; x++)
                    for (int z = 0; z < bitMap.Height; z++)
                        heightValues[x, z] = (float)data[(x + z * bitMap.Width) * 4] / 255 * ROUGHNESS;
            }
            finally
            {
                img.UnlockBits(bitMap);
            }

            numCells = vertsPerRow - 1;
            cellSize = 1f / numCells;

            int numVerts = vertsPerRow * vertsPerRow;
            positions = new Vector3f[numVerts];
            normals = new Vector3f[numVerts];
            tangents = new Vector4f[numVerts];
            texCoords = new Vector2f[numVerts];
            indices = new uint[(vertsPerRow - 1) * (vertsPerRow - 1) * 6];
            physicalShapeVerts = new Vector3f[vertsPerRow - 1, vertsPerRow - 1][];

            for (int x = 0; x < vertsPerRow; x++)
                for (int z = 0; z < vertsPerRow; z++)
                {
                    positions[x + z * vertsPerRow] = new Vector3f(x * cellSize - 0.5f, heightValues[x, z], z * cellSize - 0.5f);
                    texCoords[x + z * vertsPerRow] = new Vector2f((float)x / vertsPerRow, (float)z / vertsPerRow) * NUM_TEX_REPEATS;
                }
            int indexCounter = 0;
            for (int x = 0; x < vertsPerRow; x++)
                for (int z = 0; z < vertsPerRow; z++)
                {
                    Vector3f tan = x < vertsPerRow - 1 ? positions[(x + 1) + z * vertsPerRow] - positions[x + z * vertsPerRow] : new Vector3f(1, 0, 0);
                    tan += x > 0 ? positions[x + z * vertsPerRow] - positions[(x - 1) + z * vertsPerRow] : new Vector3f();
                    Vector3f bi = z < vertsPerRow - 1 ? positions[x + (z + 1) * vertsPerRow] - positions[x + z * vertsPerRow] : new Vector3f(0, 0, 1);
                    bi += z > 0 ? positions[x + z * vertsPerRow] - positions[x + (z - 1) * vertsPerRow] : new Vector3f();
                    tan.Normalize();
                    bi.Normalize();
                    int index = x + z * vertsPerRow;
                    normals[index] = Vector3f.Cross(bi, tan);
                    tangents[index] = new Vector4f(tan, 1);

                    if (x < vertsPerRow - 1 && z < vertsPerRow - 1)
                    {
                        uint tli = (uint)(x + z * vertsPerRow);
                        uint tri = (uint)((x + 1) + z * vertsPerRow);
                        uint bli = (uint)(x + (z + 1) * vertsPerRow);
                        uint bri = (uint)((x + 1) + (z + 1) * vertsPerRow);

                        bool flip = (positions[tli] + positions[bri]).y > (positions[tri] + positions[bli]).y;
                        indices[indexCounter++] = tli;
                        indices[indexCounter++] = flip ? bri : bli;
                        indices[indexCounter++] = tri;
                        indices[indexCounter++] = flip ? tli : tri;
                        indices[indexCounter++] = bli;
                        indices[indexCounter++] = bri;

                        physicalShapeVerts[x, z] = new Vector3f[] {
                            new Vector3f(x * cellSize - 0.5f, heightValues[x, z], z * cellSize - 0.5f),
                            new Vector3f((x + 1) * cellSize - 0.5f, heightValues[x + 1, z], z * cellSize - 0.5f),
                            new Vector3f(x * cellSize - 0.5f, heightValues[x, (z + 1)], (z + 1) * cellSize - 0.5f),
                            new Vector3f((x + 1) * cellSize - 0.5f, heightValues[x + 1, z + 1], (z + 1) * cellSize - 0.5f),
                            new Vector3f(x * cellSize - 0.5f, LOW_CUT, z * cellSize - 0.5f),
                            new Vector3f((x + 1) * cellSize - 0.5f, LOW_CUT, z * cellSize - 0.5f),
                            new Vector3f(x * cellSize - 0.5f, LOW_CUT, (z + 1) * cellSize - 0.5f),
                            new Vector3f((x + 1) * cellSize - 0.5f, LOW_CUT, (z + 1) * cellSize - 0.5f)};
                    }
                }
        }

        public Mesh CreateMesh(Graphics graphics)
            => new Mesh(
                graphics.dispatcher,
                new ChaosFramework.Shapes.MeshData(positions, normals, tangents, new Vector2f[][] { texCoords }, indices)
                );
    }
}
