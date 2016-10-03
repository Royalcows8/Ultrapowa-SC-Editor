using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using MathNet.Numerics.LinearAlgebra;

namespace UCSScEditor
{
    public class Shape : ScObject
    {
        #region Constructors
        public Shape(ScFile scs)
        {
            _scFile = scs;
            _chunks = new List<ScObject>();
        }

        public Shape(Shape s)
        {
            _scFile = s.GetStorageObject();
            _chunks = new List<ScObject>();

            this.SetOffset(-Math.Abs(s.GetOffset()));

            //Duplicate Shape
            using (FileStream input = new FileStream(_scFile.GetFileName(), FileMode.Open))
            {
                input.Seek(Math.Abs(s.GetOffset()) + 5, SeekOrigin.Begin);
                using (var br = new BinaryReader(input))
                {
                    this.ParseData(br);
                }
            }
            foreach (ShapeChunk chunk in this._chunks)
            {
                chunk.SetOffset(-Math.Abs(chunk.GetOffset()));
            }
        }
        #endregion

        #region Fields & Properties
        private short _shapeId;
        private int _length;
        private List<ScObject> _chunks;
        private ScFile _scFile;
        private long _offset;

        public override List<ScObject> Children => _chunks;
        #endregion

        #region Methods
        public override int GetDataType()
        {
            return 0;
        }

        public override string GetDataTypeName()
        {
            return "Shapes";
        }

        public override string GetName()
        {
            return "Shape " + GetId().ToString();
        }

        public List<ScObject> GetChunks()
        {
            return _chunks;
        }

        public override short GetId()
        {
            return _shapeId;
        }

        public override string GetInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("/!\\ Experimental Rendering");
            sb.AppendLine("");
            sb.AppendLine("ShapeId: " + _shapeId);
            sb.AppendLine("Polygons: " + _chunks.Count);
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

        public override bool IsImage()
        {
            return true;
        }

        public override void ParseData(BinaryReader br)
        {
            /*
            StringBuilder hex = new StringBuilder(data.Length * 2);
            foreach (byte b in data)
                hex.AppendFormat("{0:x2}", b);
            Debug.WriteLine(hex.ToString());
            */

            _shapeId = br.ReadInt16(); // 0000
            br.ReadUInt16(); // 0100
            br.ReadUInt16(); // 0400 if datatype 18

            while (true)
            {
                byte chunkType;
                while (true)
                {
                    chunkType = br.ReadByte(); // 11
                    _length = br.ReadInt32(); // 32000000
                    if (chunkType == 17 || chunkType == 22)
                    {
                        ShapeChunk chunk = new ShapeChunk(_scFile);
                        chunk.SetChunkId((short)_chunks.Count);
                        chunk.SetShapeId(_shapeId);
                        chunk.SetChunkType(chunkType);
                        chunk.ParseData(br);

                        _chunks.Add(chunk);
                    }
                    else
                    {
                        break;
                    }
                }
                if (chunkType == 0)
                    break;
                Debug.WriteLine("Unmanaged chunk type " + chunkType);
                br.ReadBytes(_length);
            }
        }

        public override Bitmap Render(RenderingOptions options)
        {
            /*
            Debug.WriteLine("XY:");
            foreach(ShapeChunk chunk in m_vChunks)
            {
                foreach(var p in chunk.GetPointsXY())
                {
                    Debug.WriteLine("x: " + p.X + ", y: " + p.Y);
                }
                Debug.WriteLine("");
            }

            foreach (ShapeChunk chunk in m_vChunks)
            {
                foreach (var p in chunk.GetPointsUV())
                {
                    Debug.WriteLine("u: " + p.X + ", u: " + p.Y);
                }
                Debug.WriteLine("");
            }
            */

            Debug.WriteLine("Rendering image of " + _chunks.Count.ToString() + " polygons");

            //Calculate et initialize the final shape size
            List<PointF> pointsXY = _chunks.SelectMany(chunk => ((ShapeChunk)chunk).GetPointsXY()).ToList();
            GraphicsPath pathXY = new GraphicsPath();
            pathXY.AddPolygon(pointsXY.ToArray());
            int width = Rectangle.Round(pathXY.GetBounds()).Width;
            width = width > 0 ? width : 1;
            int height = Rectangle.Round(pathXY.GetBounds()).Height;
            height = height > 0 ? height : 1;
            int x = Rectangle.Round(pathXY.GetBounds()).X;
            int y = Rectangle.Round(pathXY.GetBounds()).Y;
            var finalShape = new Bitmap(width, height);

            //Assemble shape chunks
            foreach (ShapeChunk chunk in _chunks)
            {
                var texture = (Texture)_scFile.GetTextures()[chunk.GetTextureId()];
                Bitmap bitmap = texture.Bitmap;

                var polygonUV = chunk.GetPointsUV();
                var polygonXY = chunk.GetPointsXY();

                GraphicsPath gpuv = new GraphicsPath();
                gpuv.AddPolygon(polygonUV.ToArray());

                GraphicsPath gpxy = new GraphicsPath();
                gpxy.AddPolygon(polygonXY.ToArray());

                int gpuvWidth = Rectangle.Round(gpuv.GetBounds()).Width;
                gpuvWidth = gpuvWidth > 0 ? gpuvWidth : 1;
                int gpuvHeight = Rectangle.Round(gpuv.GetBounds()).Height;
                gpuvHeight = gpuvHeight > 0 ? gpuvHeight : 1;
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
                }

                GraphicsPath gp = new GraphicsPath();
                gp.AddPolygon(new Point[] { new Point(0, 0), new Point(gpuvWidth, 0), new Point(0, gpuvHeight) });

                //Calculate transformation Matrix of UV
                //double[,] matrixArrayUV = { { polygonUV[0].X, polygonUV[1].X, polygonUV[2].X }, { polygonUV[0].Y, polygonUV[1].Y, polygonUV[2].Y }, { 1, 1, 1 } };
                double[,] matrixArrayUV = { { gpuv.PathPoints[0].X, gpuv.PathPoints[1].X, gpuv.PathPoints[2].X }, { gpuv.PathPoints[0].Y, gpuv.PathPoints[1].Y, gpuv.PathPoints[2].Y }, { 1, 1, 1 } };
                double[,] matrixArrayXY = { { polygonXY[0].X, polygonXY[1].X, polygonXY[2].X }, { polygonXY[0].Y, polygonXY[1].Y, polygonXY[2].Y }, { 1, 1, 1 } };
                var matrixUV = Matrix<double>.Build.DenseOfArray(matrixArrayUV);
                var matrixXY = Matrix<double>.Build.DenseOfArray(matrixArrayXY);
                var inverseMatrixUV = matrixUV.Inverse();
                var transformMatrix = matrixXY * inverseMatrixUV;
                var m = new Matrix((float)transformMatrix[0, 0], (float)transformMatrix[1, 0], (float)transformMatrix[0, 1], (float)transformMatrix[1, 1], (float)transformMatrix[0, 2], (float)transformMatrix[1, 2]);
                //var m = new Matrix((float)transformMatrix[0, 0], (float)transformMatrix[1, 0], (float)transformMatrix[0, 1], (float)transformMatrix[1, 1], (float)Math.Round(transformMatrix[0, 2]), (float)Math.Round(transformMatrix[1, 2]));

                //Perform transformations
                gp.Transform(m);

                using (Graphics g = Graphics.FromImage(finalShape))
                {
                    //g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    //g.PixelOffsetMode = PixelOffsetMode.None;

                    //Set origin
                    Matrix originTransform = new Matrix();
                    originTransform.Translate(-x, -y);
                    g.Transform = originTransform;

                    g.DrawImage(shapeChunk, gp.PathPoints, gpuv.GetBounds(), GraphicsUnit.Pixel);

                    if (options.ViewPolygons)
                    {
                        gpuv.Transform(m);
                        g.DrawPath(new Pen(Color.DarkGray, 1), gpuv);
                    }
                }
            }
            return finalShape;
        }

        public override void Save(FileStream input)
        {
            if (_offset < 0)//new
            {
                using (FileStream readInput = new FileStream(_scFile.GetFileName(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    //Positionnement des curseurs
                    readInput.Seek(Math.Abs(_offset), SeekOrigin.Begin);
                    input.Seek(_scFile.GetEofOffset(), SeekOrigin.Begin);

                    //type and length
                    byte[] dataType = new byte[1];
                    readInput.Read(dataType, 0, 1);
                    byte[] dataLength = new byte[4];
                    readInput.Read(dataLength, 0, 4);
                    input.Write(dataType, 0, 1);
                    input.Write(dataLength, 0, 4);

                    //shape
                    readInput.Seek(2, SeekOrigin.Current);
                    input.Write(BitConverter.GetBytes(_shapeId), 0, 2);

                    byte[] unknown1 = new byte[2];
                    readInput.Read(unknown1, 0, 2);//0100
                    input.Write(unknown1, 0, 2);

                    byte[] unknown2 = new byte[2];
                    readInput.Read(unknown2, 0, 2);//0400
                    input.Write(unknown2, 0, 2);

                    int chunkCounter = 0;
                    while (true)
                    {
                        byte shapeType;
                        byte[] length = new byte[4];
                        while (true)
                        {
                            shapeType = (byte)readInput.ReadByte();//11
                            input.WriteByte(shapeType);

                            readInput.Read(length, 0, 4);//32000000
                            input.Write(length, 0, 4);

                            if (shapeType == 17)
                            {
                                _chunks[chunkCounter].Save(input);
                                chunkCounter++;
                                readInput.Seek(BitConverter.ToInt32(length, 0), SeekOrigin.Current);
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (shapeType == 0)
                        {
                            break;
                        }
                        Debug.WriteLine("Unmanaged shape type " + shapeType);
                        for (int i = 0; i < BitConverter.ToInt32(length, 0); i++)
                        {
                            input.WriteByte((byte)readInput.ReadByte());
                        }
                    }
                }
                _offset = _scFile.GetEofOffset();
                _scFile.SetEofOffset(input.Position);
                input.Write(new byte[] { 0, 0, 0, 0, 0 }, 0, 5);
            }
        }

        public void SetId(short id)
        {
            _shapeId = id;
        }

        public void SetOffset(long offset)
        {
            _offset = offset;
        }
        #endregion
    }
}
