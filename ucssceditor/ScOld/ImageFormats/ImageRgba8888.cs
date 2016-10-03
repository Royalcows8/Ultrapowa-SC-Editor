using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace UCSScEditor.ScOld
{
    internal class ImageRgba8888 : ScImage
    {
        #region Constructors
        public ImageRgba8888()
        {
            // Space
        }
        #endregion

        #region Methods
        public unsafe override void ReadImage(BinaryReader br, BinaryReader texbr)
        {
            base.ReadImage(br, texbr);

            var sw = Stopwatch.StartNew();
            _bitmap = new Bitmap(_width, _height, PixelFormat.Format32bppArgb);
            texbr.ReadBytes(10);

            var rect = new Rectangle(0, 0, _width, _height);
            var data = _bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            // Size of the bitmap in memory. Each pixel occupies 4 bytes.
            var length = data.Stride * data.Height;

            // Read all the bytes into a byte array and read it using pointers,
            // this drastically improves performance.
            // Bitmap from BinaryReader in bytes.
            var sourceBytes = texbr.ReadBytes(length);

            // Pointer to the beginning of the destination bitmap in memory.
            byte* dst = (byte*)data.Scan0.ToPointer();
            // Pointer to the beginning of the source bitmap in memory.
            fixed (byte* fixSrc = sourceBytes)
            {
                // Copies bytes at src to bytes at dst.
                for (byte* src = fixSrc; src < fixSrc + length; src += 4)
                {
                    var r = *(src);
                    var g = *(src + 1);
                    var b = *(src + 2);
                    var a = *(src + 3);

                    *(dst) = b;
                    *(dst + 1) = g;
                    *(dst + 2) = r;
                    *(dst + 3) = a;

                    dst += 4;
                }
            }
            _bitmap.UnlockBits(data);

            sw.Stop();
            Debug.WriteLine("ImageRgba8888.ReadImage finished in {0}ms", sw.Elapsed.TotalMilliseconds);
            _bitmap.Save("kek" + _width + ".png");
        }

        public override void WriteImage(FileStream input)
        {
            //TODO: Implement unsafe writing.
            base.WriteImage(input);

            for (int column = 0; column < _bitmap.Height; column++)
            {
                for (int row = 0; row < _bitmap.Width; row++)
                {
                    input.WriteByte(_bitmap.GetPixel(row, column).R);
                    input.WriteByte(_bitmap.GetPixel(row, column).G);
                    input.WriteByte(_bitmap.GetPixel(row, column).B);
                    input.WriteByte(_bitmap.GetPixel(row, column).A);
                }
            }
        }

        public override string GetImageTypeName()
        {
            return "RGB8888";
        }
        #endregion
    }
}
