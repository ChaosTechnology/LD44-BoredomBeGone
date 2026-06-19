using ChaosFramework.Math.Vectors;
using ChaosUtil.Serialization.Text;
using System;
using System.IO;
using System.Reflection;

namespace LD44
{
    public class Settings
    {
        // TODO: use ChaosUtil.Serialization.Text.Ini for this

        public const string FILE = "Settings.confix";

        public void Save(StreamWriter wr)
        {
            foreach (FieldInfo info in typeof(Settings).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                wr.Write(info.Name + ": ");
                wr.WriteLine(info.GetValue(this).ToString());
            }
        }

        public static Settings Load(string file)
        {
            using (FileStream str = new FileStream(file, FileMode.Open))
            using (StreamReader rd = new StreamReader(str))
                return Load(rd);
        }

        public static Settings Load(StreamReader rd)
        {
            Settings settings = new Settings();
            settings._Load(rd);
            return settings;
        }

        public Vector2i deferredShaderSize = -1;
        public float transparencyScaling = 1;
        public int transparencyLayers = 20;
        public int maxFPS = 60;

        public void Save(string file)
        {
            using (FileStream str = new FileStream(file, FileMode.Create))
            using (StreamWriter wr = new StreamWriter(str))
                Save(wr);
        }

        void _Load(StreamReader rd)
        {
            while (!rd.EndOfStream)
            {
                string line = rd.ReadLine().Trim();
                if (line == string.Empty || line.StartsWith("//"))
                    continue;

                string[] nameAndValue = line.Split(new char[] { ':' }, 2);
                for (int i = 0; i < nameAndValue.Length; i++)
                    nameAndValue[i] = nameAndValue[i].Trim();

                FieldInfo info = typeof(Settings).GetField(nameAndValue[0], BindingFlags.Public | BindingFlags.Instance);
                if (info == null)
                    throw new Exception("unknown setting \"" + nameAndValue[0] + "\".");

                Delegate parser = null;
                if (!Parse.TryGetParser(info.FieldType, out parser))
                    throw new Exception("cannot parse type \"" + info.FieldType.Name + "\"");

                var args = new object[] { nameAndValue[1], null };
                if (!(bool)parser.DynamicInvoke(args))
                    throw new Exception("cannot parse \"" + nameAndValue[1] + "\" to \"" + info.FieldType.Name + "\"");

                info.SetValue(this, args[1]);
            }
        }
    }
}
