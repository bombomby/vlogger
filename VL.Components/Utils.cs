using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.Components
{
    public class Utils
    {
        public static NumberFormatInfo NumberFormat = new NumberFormatInfo() { NumberGroupSeparator = " " };

		/// <summary>
		/// Deterministic version of string hash (Jenkins)
		/// </summary>
        public static uint GetHashCode(String key)
        {
			uint hash = 0;
			for (int i = 0; i < key.Length; ++i)
			{
				hash += key[(int)i];
				hash += (hash << 10);
				hash ^= (hash >> 6);
			}
			hash += (hash << 3);
			hash ^= (hash >> 11);
			hash += (hash << 15);
			
			return hash;
		}



		const float DarkLuminanceThreshold = 0.2f;

		public static Color GetRandomColor(String name, float threshold = DarkLuminanceThreshold)
        {
			Random rnd = new Random((int)GetHashCode(name));
			Color c;
			do {
				c = Color.FromArgb((int)((uint)rnd.Next() | 0xFF000000));
			} while (GetLuminance(c) < threshold);
			return c;
        }
			
		public static float GetLuminance(Color color)
		{
			return (0.2126f * color.R + 0.7152f * color.G + 0.0722f * color.B) / 255.0f;
		}

		public static string ToStringNoAlpha(Color color)
        {
			return String.Format("{0:X6}", (uint)color.ToArgb() & 0x00FFFFFF);
        }

		public static String TrimCommandFromArgs(String args)
		{
			if (args.Length > 0)
			{
				if (args.StartsWith('\''))
					return args.Substring(args.IndexOf('\'', 1) + 1);

				if (args.StartsWith('\"'))
					return args.Substring(args.IndexOf('\"', 1) + 1);

				return args.Substring(args.IndexOf(' ') + 1);
			}
			return args;
		}
	}
}
