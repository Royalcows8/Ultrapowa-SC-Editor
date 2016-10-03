using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace UCSScEditor
{
    internal class ImageRgb565 : ScImage
    {
        public ImageRgb565()
        {
            // Space
        }

        public override string GetImageTypeName()
        {
            return "RGB565";
        }

        public override void ReadImage(BinaryReader br)
        {
            base.ReadImage(br);
            _bitmap = new Bitmap(_width, _height, PixelFormat.Format32bppArgb);

            for (int column = 0; column < _height; column++)
            {
                for (int row = 0; row < _width; row++)
                {
                    ushort color = br.ReadUInt16();

                    int red = (int)((color >> 11) & 0x1F) << 3;
                    int green = (int)((color >> 5) & 0x3F) << 2;
                    int blue = (int)(color & 0X1F) << 3;

                    _bitmap.SetPixel(row, column, Color.FromArgb(red, green, blue));
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

                    ushort color = (ushort)(((((red >> 3)) & 0x1F) << 11) | ((((green >> 2)) & 0x3F) << 5) | ((blue >> 3) & 0x1F));

                    input.Write(BitConverter.GetBytes(color), 0, 2);
                }
            }
        }
    }
}
