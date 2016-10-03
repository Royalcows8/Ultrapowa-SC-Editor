using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

namespace UCSScEditor.ScOld
{
    public class Texture : ScObject
    {
        #region Constants
        private static readonly Dictionary<byte, Type> s_imageTypes;
        #endregion

        #region Constructors
        static Texture()
        {
            s_imageTypes = new Dictionary<byte, Type>();
            s_imageTypes.Add(0, typeof(ImageRgba8888));
            s_imageTypes.Add(2, typeof(ImageRgba4444));
            s_imageTypes.Add(4, typeof(ImageRgb565));
        }

        public Texture(ScFile scs)
        {
            _scFile = scs;
            _textureId = (short)_scFile.GetTextures().Count();
        }

        public Texture(Texture t)
        {
            _imageType = t.GetImageType();
            _scFile = t.GetStorageObject();
            _textureId = (short)_scFile.GetTextures().Count();
            if (s_imageTypes.ContainsKey(_imageType))
            {
                _image = (ScImage)Activator.CreateInstance(s_imageTypes[_imageType]);
            }
            else
            {
                _image = new ScImage();
            }
            _image.SetBitmap(new Bitmap(t.Bitmap));
            _offset = t.GetOffset() > 0 ? -t.GetOffset() : t.GetOffset();
        }
        #endregion

        #region Fields & Properties
        private byte _imageType;
        private short _textureId;

        private ScFile _scFile;
        private ScImage _image;
        private long _offset;

        public override Bitmap Bitmap => _image.GetBitmap();
        #endregion

        #region Methods
        public override short Id => _textureId;

        public override int GetDataType()
        {
            return 2;
        }

        public override string GetDataTypeName()
        {
            return "Textures";
        }

        internal ScImage GetImage()
        {
            return _image;
        }

        public byte GetImageType()
        {
            return _imageType;
        }

        public override string GetInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("TextureId: " + _textureId);
            sb.AppendLine("ImageType: " + _imageType.ToString());
            sb.AppendLine("ImageFormat: " + _image.GetImageTypeName());
            sb.AppendLine("Width: " + _image.GetWidth());
            sb.AppendLine("Height: " + _image.GetHeight());
            return sb.ToString();
        }

        public long GetOffset()
        {
            return _offset;
        }

        public ScFile GetStorageObject()
        {
            return _scFile;
        }

        public short GetTextureId()
        {
            return _textureId;
        }

        public override bool IsImage()
        {
            return true;
        }

        public override void Read(BinaryReader br)
        {
            _imageType = br.ReadByte();

            if (s_imageTypes.ContainsKey(_imageType))
                _image = (ScImage)Activator.CreateInstance(s_imageTypes[_imageType]);
            else
                _image = new ScImage();

            _image.ReadImage(br, null);
        }

        public void ReadW(BinaryReader br, BinaryReader texbr)
        {
            _imageType = br.ReadByte();

            if (s_imageTypes.ContainsKey(_imageType))
                _image = (ScImage)Activator.CreateInstance(s_imageTypes[_imageType]);
            else
                _image = new ScImage();

            _image.ReadImage(br, texbr);
        }

        public override Bitmap Render(RenderingOptions options)
        {
            return Bitmap;
        }

        public override void Write(FileStream input)
        {
            if (_offset < 0) // New
            {
                // Get info
                input.Seek(Math.Abs(_offset), SeekOrigin.Begin);
                byte[] dataType = new byte[1];
                input.Read(dataType, 0, 1);
                byte[] dataLength = new byte[4];
                input.Read(dataLength, 0, 4);

                // Then write
                input.Seek(_scFile.GetEofOffset(), SeekOrigin.Begin);
                input.Write(dataType, 0, 1);
                input.Write(dataLength, 0, 4);
                input.WriteByte(_imageType);

                _image.WriteImage(input);
                _offset = _scFile.GetEofOffset();
                _scFile.SetEofOffset(input.Position);

                input.Write(new byte[] { 0, 0, 0, 0, 0 }, 0, 5);
            }
            else // Existing
            {
                input.Seek(_offset + 5, SeekOrigin.Current);
                input.WriteByte(_imageType);
                _image.WriteImage(input);
            }
        }

        public void SetOffset(long position)
        {
            _offset = position;
        }
        #endregion
    }
}
