using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace UCSScEditor.ScOld
{
    public class ShapeChunk : ScObject
    {
        #region Constructors
        public ShapeChunk(ScFile scs)
        {
            _scFile = scs;
            _pointsXY = new List<PointF>();
            _pointsUV = new List<PointF>();
        }
        #endregion

        #region Fields & Properties
        private short _chunkId;
        private short _shapeId;
        private byte _textureId;
        private byte _chunkType;

        private List<PointF> _pointsXY;
        private List<PointF> _pointsUV;
        private ScFile _scFile;
        private long _offset;
        #endregion

        #region Methods
        public byte GetChunkType()
        {
            return _chunkType;
        }

        public override int GetDataType()
        {
            return 99;
        }

        public override string GetDataTypeName()
        {
            return "ShapeChunks";
        }

        public override string GetName()
        {
            return "Chunk " + Id.ToString();
        }

        public List<PointF> GetPointsUV()
        {
            return _pointsUV;
        }

        public List<PointF> GetPointsXY()
        {
            return _pointsXY;
        }

        public override short Id => _chunkId;

        public override string GetInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ChunkId: " + _chunkId);
            sb.AppendLine("ShapeId (ref): " + _shapeId);
            sb.AppendLine("TextureId (ref): " + _textureId);
            return sb.ToString();
        }

        public long GetOffset()
        {
            return _offset;
        }

        public short GetShapeId()
        {
            return _shapeId;
        }

        public byte GetTextureId()
        {
            return _textureId;
        }

        public override bool IsImage()
        {
            return true;
        }

        public override void Read(BinaryReader br)
        {
            Debug.WriteLine("Parsing chunk data from shape " + _shapeId);

            _offset = br.BaseStream.Position;
            _textureId = br.ReadByte(); // 00

            byte shapePointCount = br.ReadByte(); // 04
            var texture = (Texture)_scFile.GetTextures()[_textureId];

            for (int i = 0; i < shapePointCount; i++)
            {
                float x = (float)(br.ReadInt32() * 0.05);//* 0.05);
                float y = (float)(br.ReadInt32() * 0.05);//* 0.05);
                _pointsXY.Add(new PointF(x, y));
                Debug.WriteLine("x: " + x + ", y: " + y);
            }

            if (_chunkType == 22)
            {
                for (int i = 0; i < shapePointCount; i++)
                {
                    float u = (float)(br.ReadUInt16() / 65535.0) * texture.GetImage().GetWidth();
                    float v = (float)(br.ReadUInt16() / 65535.0) * texture.GetImage().GetHeight();
                    _pointsUV.Add(new PointF(u, v));

                    Debug.WriteLine("u: " + u + ", v: " + v);
                }
            }
            else
            {
                for (int i = 0; i < shapePointCount; i++)
                {
                    ushort u = br.ReadUInt16(); // image.Width);
                    ushort v = br.ReadUInt16(); // image.Height);//(short) (65535 * br.ReadInt16() / image.Height);
                    _pointsUV.Add(new Point(u, v));

                    Debug.WriteLine("u: " + u + ", v: " + v);
                }
            }
        }

        public override Bitmap Render(RenderingOptions options)
        {
            Debug.WriteLine("Rendering chunk from shape " + _shapeId);

            Bitmap result = null;

            var texture = (Texture)_scFile.GetTextures()[_textureId];
            if (texture != null)
            {
                Bitmap bitmap = texture.Bitmap;

                Debug.WriteLine("Rendering polygon image of " + GetPointsUV().Count.ToString() + " points");
                foreach (PointF uv in GetPointsUV())
                {
                    Debug.WriteLine("u: " + uv.X + ", v: " + uv.Y);
                }

                GraphicsPath gpuv = new GraphicsPath();
                gpuv.AddPolygon(GetPointsUV().ToArray());

                int gpuvWidth = Rectangle.Round(gpuv.GetBounds()).Width;
                gpuvWidth = gpuvWidth > 0 ? gpuvWidth : 1;

                Debug.WriteLine("gpuvWidth: " + gpuvWidth);

                int gpuvHeight = Rectangle.Round(gpuv.GetBounds()).Height;
                gpuvHeight = gpuvHeight > 0 ? gpuvHeight : 1;

                Debug.WriteLine("gpuvHeight: " + gpuvHeight);

                var shapeChunk = new Bitmap(gpuvWidth, gpuvHeight);
                int chunkX = Rectangle.Round(gpuv.GetBounds()).X;
                int chunkY = Rectangle.Round(gpuv.GetBounds()).Y;

                //bufferizing shape
                using (Graphics g = Graphics.FromImage(shapeChunk))
                {
                    //On conserve la qualité de l'image intacte
                    gpuv.Transform(new Matrix(1, 0, 0, 1, -chunkX, -chunkY));
                    g.SetClip(gpuv);
                    g.DrawImage(bitmap, -chunkX, -chunkY);
                    if (options.ViewPolygons)
                        g.DrawPath(new Pen(Color.DarkGray, 2), gpuv);
                }

                result = shapeChunk;
            }
            return result;
        }

        public void Replace(Bitmap chunk)
        {
            var texture = (Texture)_scFile.GetTextures()[_textureId];
            if (texture != null)
            {
                Bitmap bitmap = texture.Bitmap;

                GraphicsPath gpuv = new GraphicsPath();
                gpuv.AddPolygon(GetPointsUV().ToArray());
                int x = Rectangle.Round(gpuv.GetBounds()).X;
                int y = Rectangle.Round(gpuv.GetBounds()).Y;
                int width = Rectangle.Round(gpuv.GetBounds()).Width;
                int height = Rectangle.Round(gpuv.GetBounds()).Height;

                GraphicsPath gpChunk = new GraphicsPath();
                gpChunk.AddRectangle(new Rectangle(0, 0, width, height));

                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    gpChunk.Transform(new Matrix(1, 0, 0, 1, x, y));
                    g.SetClip(gpuv);
                    g.Clear(Color.Transparent);
                    g.DrawImage(chunk, x, y);
                }
            }
        }

        public override void Write(FileStream input)
        {
            if (_offset < 0)
            {
                _offset = input.Position;
                input.WriteByte(_textureId);
                input.WriteByte((byte)_pointsUV.Count);
                foreach (var pointXY in _pointsXY)
                {
                    input.Write(BitConverter.GetBytes((int)(pointXY.X * 20)), 0, 4);
                    input.Write(BitConverter.GetBytes((int)(pointXY.Y * 20)), 0, 4);
                }

                var texture = (Texture)_scFile.GetTextures()[_textureId];

                if (_chunkType == 22)
                {
                    foreach (var pointUV in _pointsUV)
                    {
                        input.Write(BitConverter.GetBytes((ushort)((pointUV.X / texture.GetImage().GetWidth()) * 65535)), 0, 2);
                        input.Write(BitConverter.GetBytes((ushort)((pointUV.Y / texture.GetImage().GetHeight()) * 65535)), 0, 2);
                    }
                }
                else
                {
                    foreach (var pointUV in _pointsUV)
                    {
                        input.Write(BitConverter.GetBytes((ushort)(pointUV.X)), 0, 2);
                        input.Write(BitConverter.GetBytes((ushort)(pointUV.Y)), 0, 2);
                    }
                }
            }
            else
            {
                input.Seek(_offset, SeekOrigin.Begin);
                input.WriteByte(_textureId);
            }
        }

        public void SetChunkId(short id)
        {
            _chunkId = id;
        }

        public void SetChunkType(byte type)
        {
            _chunkType = type;
        }

        public void SetOffset(long offset)
        {
            _offset = offset;
        }

        public void SetShapeId(short id)
        {
            _shapeId = id;
        }

        public void SetTextureId(byte id)
        {
            _textureId = id;
        }
        #endregion
    }
}
