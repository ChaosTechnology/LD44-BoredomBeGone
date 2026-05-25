using ChaosFramework.Math.Vectors;

namespace LD44
{
    public class Parsers
    {
        public static bool ParseVector2i(string str, out Vector2i value)
        {
            str = str.Trim();
            value = new Vector2i();

            if (str.StartsWith("{") && str.EndsWith("}"))
                str = str.Substring(1, str.Length - 2);
            else if (str.StartsWith("{") || str.EndsWith("}"))
                return false;

            string[] val = str.Split(new char[] { ';' });
            return val.Length == 2
                && int.TryParse(val[0], out value.x)
                && int.TryParse(val[1], out value.y)
                ;
        }
    }
}
