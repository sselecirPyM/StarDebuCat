using SC2APIProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace StarDebuCat.Algorithm
{
    public class Image
    {
        public byte[] Data;
        public int Width;
        public int Height;
        public int BitsPerPixel;
        public Image(ImageData imageData)
        {
            Data = imageData.Data.ToByteArray();
            Width = imageData.Size.X;
            Height = imageData.Size.Y;
            BitsPerPixel = imageData.BitsPerPixel;
        }
        public Image(Image imageData)
        {
            Data = imageData.Data.ToArray();
            Width = imageData.Width;
            Height = imageData.Height;
            BitsPerPixel = imageData.BitsPerPixel;
        }
        public byte Query(Vector2 p) => Query((int)(p.X + 0.5f), (int)(p.Y + 0.5f));

        public byte Query(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return 0;
            }
            if (BitsPerPixel == 1)
            {
                int pixelID = x + y * Width;
                int byteLocation = pixelID / 8;
                int bitLocation = pixelID % 8;
                var result = ((Data[byteLocation] & 1 << (7 - bitLocation)) == 0) ? 0 : 1;
                return (byte)((result == 0) ? 0 : 1);
            }
            else
            {
                int pixelID = x + y * Width;
                int byteLocation = pixelID;
                return Data[byteLocation];
            }
        }
        public bool Write((int, int) p, bool value) => Write(p.Item1, p.Item2, value);
        public bool Write(int x, int y, bool value)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return false;
            }
            int pixelID = x + y * Width;
            int byteLocation = pixelID / 8;
            int bitLocation = pixelID % 8;

            if (value)
            {
                Data[byteLocation] = (byte)(Data[byteLocation] | 1 << (7 - bitLocation));
            }
            else
            {
                Data[byteLocation] = (byte)(Data[byteLocation] & ~(1 << (7 - bitLocation)));
            }

            return true;
        }

        public bool RectCheck((int, int) pos, (int, int) size)
        {
            for (int x = 0; x < size.Item1; x++)
                for (int y = 0; y < size.Item2; y++)
                {
                    if (Query(pos.Item1 + x, pos.Item2 + y) == 0)
                        return false;
                }
            return true;
        }
    }
}
