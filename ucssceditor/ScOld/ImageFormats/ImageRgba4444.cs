using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace UCSScEditor.ScOld
{
    internal class ImageRgba4444 : ScImage
    {
        public ImageRgba4444()
        {
            // Space
        }

        public override string GetImageTypeName()
        {
            return "RGB4444";
        }

        public override void ReadImage(BinaryReader br, BinaryReader texbr)
        {
            base.ReadImage(br, texbr);

            _bitmap = new Bitmap(_width, _height, PixelFormat.Format32bppArgb);

            for (int column = 0; column < _height; column++)
            {
                for (int row = 0; row < _width; row++)
                {
                    ushort color = br.ReadUInt16();

                    int red = (int)((color >> 12) & 0xF) << 4;
                    int green = (int)((color >> 8) & 0xF) << 4;
                    int blue = (int)((color >> 4) & 0xF) << 4;
                    int alpha = (int)(color & 0xF) << 4;

                    _bitmap.SetPixel(row, column, Color.FromArgb(alpha, red, green, blue));
                }
            }
        }

        public override void Print()
        {
            base.Print();
        }

        public override void WriteImage(FileStream input)
        {
            base.WriteImage(input);
            for (int column = 0; column < _bitmap.Height; column++)
            {
                for (int row = 0; row < _bitmap.Width; row++)
                {
                    byte red = _bitmap.GetPixel(row, column).R;
                    byte green = _bitmap.GetPixel(row, column).G;
                    byte blue = _bitmap.GetPixel(row, column).B;
                    byte alpha = _bitmap.GetPixel(row, column).A;

                    ushort color = (ushort)(((((red >> 4)) & 0xF) << 12) | ((((green >> 4)) & 0xF) << 8) | ((((blue >> 4)) & 0xF) << 4) | ((alpha >> 4) & 0xF));

                    input.Write(BitConverter.GetBytes(color), 0, 2);
                }
            }
        }
    }
}
