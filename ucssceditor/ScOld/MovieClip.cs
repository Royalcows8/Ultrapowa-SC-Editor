using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace UCSScEditor.ScOld
{
    public class MovieClip : ScObject
    {
        #region Constructors
        public MovieClip(ScFile scs, short dataType)
        {
            _scFile = scs;
            _dataType = dataType;
            _shapes = new List<ScObject>();
        }

        public MovieClip(MovieClip mv)
        {
            _scFile = mv.GetStorageObject();
            _dataType = mv.GetMovieClipDataType();
            _shapes = new List<ScObject>();

            this.SetOffset(-Math.Abs(mv.GetOffset()));
            
            //Duplicate MovieClip
            using (FileStream input = new FileStream(_scFile.GetFileName(), FileMode.Open))
            {
                input.Seek(Math.Abs(mv.GetOffset()) + 5, SeekOrigin.Begin);
                using (var br = new BinaryReader(input))
                {
                    this.Read(br);
                }
            }

            //Set new clip id
            short maxMovieClipId = this.Id;
            foreach(MovieClip clip in _scFile.GetMovieClips())
            {
                if (clip.Id > maxMovieClipId)
                    maxMovieClipId = clip.Id;
            }
            maxMovieClipId++;
            this.SetId(maxMovieClipId);

            //Get max shape id
            short maxShapeId = 20000;//avoid collision with other objects in MovieClips
            foreach(Shape shape in _scFile.GetShapes())
            {
                if (shape.Id > maxShapeId)
                    maxShapeId = shape.Id;
            }
            maxShapeId++;

            //Duplicate shapes associated to clip
            List<ScObject> newShapes = new List<ScObject>();
            foreach(Shape s in _shapes)
            {
                Shape newShape = new Shape(s);
                newShape.SetId(maxShapeId);
                maxShapeId++;
                newShapes.Add(newShape);
                _scFile.AddShape(newShape);//Add to global shapelist
                _scFile.AddChange(newShape);
            }
            this._shapes = newShapes;
        }
        #endregion

        #region Fields & Properties
        private short _dataType;
        private short _clipId;
        private short _frameCount;
        private List<ScObject> _shapes;
        private ScFile _scFile;
        private long _offset;

        public override short Id => _clipId;
        public override List<ScObject> Children => _shapes;
        #endregion

        #region Methods
        public override int GetDataType()
        {
            return 1;
        }

        public override string GetDataTypeName()
        {
            return "MovieClips";
        }

        public short GetMovieClipDataType()
        {
            return _dataType;
        }

        public long GetOffset()
        {
            return _offset;
        }

        public List<ScObject> GetShapes()
        {
            return _shapes;
        }

        public ScFile GetStorageObject()
        {
            return _scFile;
        }

        public override void Read(BinaryReader br)
        {            
            Debug.WriteLine("MovieClip data type: " + _dataType);
            /*StringBuilder hex = new StringBuilder(data.Length * 2);
            foreach (byte b in data)
                hex.AppendFormat("{0:x2}", b);
            Debug.WriteLine(hex.ToString());*/

            _clipId = br.ReadInt16();
            br.ReadByte();//a1 + 34
            _frameCount = br.ReadInt16();
            if(_dataType == 14)
            {
                    
            }
            else
            {
                int cnt1 = br.ReadInt32();
                short[] sa1 = new short[cnt1 * 3];
                for(int i=0;i<cnt1;i++)
                {
                    sa1[3 * i] = br.ReadInt16();
                    sa1[3 * i + 1] = br.ReadInt16();
                    sa1[3 * i + 2] = br.ReadInt16();
                }
            }
            int cnt2 = br.ReadInt16();
            short[] sa2 = new short[cnt2];//a1 + 8
            for (int i = 0; i < cnt2; i++)
            {
                sa2[i] = br.ReadInt16();
                int index = _scFile.GetShapes().FindIndex(shape => shape.Id == sa2[i]);
                if (index != -1)
                    _shapes.Add(_scFile.GetShapes()[index]);
            }
            if (_dataType == 12)
                br.ReadBytes(cnt2);//a1 + 12

            //read ascii
            for(int i=0;i<cnt2;i++)
            {
                byte stringLength = br.ReadByte();
                if (stringLength < 255)
                    br.ReadBytes(stringLength);
            }
                
            byte v26;
            while(true)
            {
                while(true)
                {
                    while(true)
                    {
                        v26 = br.ReadByte();
                        br.ReadInt32();
                        if (v26 != 5)
                            break;
                    }
                    if (v26 == 11)
                    {
                        short frameId = br.ReadInt16();
                        byte frameNameLength = br.ReadByte();
                        if (frameNameLength < 255)
                            br.ReadBytes(frameNameLength);
                    }
                    else
                        break;
                }
                if (v26 == 0)
                    break;
                Debug.WriteLine("Unknown tag " + v26.ToString());
            }
        }

        public override void Write(FileStream input)
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

                    //movieclip
                    readInput.Seek(2, SeekOrigin.Current);
                    input.Write(BitConverter.GetBytes(_clipId), 0, 2);

                    input.WriteByte((byte)readInput.ReadByte());
                    readInput.Seek(2, SeekOrigin.Current);
                    input.Write(BitConverter.GetBytes(_frameCount), 0, 2);

                    if (_dataType == 14)
                    {

                    }
                    else
                    {
                        //int cnt1 = br.ReadInt32();
                        byte[] cnt1 = new byte[4];
                        readInput.Read(cnt1, 0, 4);
                        input.Write(cnt1, 0, 4);

                        for (int i = 0; i < BitConverter.ToInt32(cnt1, 0); i++)
                        {
                            byte[] uk1 = new byte[2];
                            readInput.Read(uk1, 0, 2);
                            input.Write(uk1, 0, 2);

                            byte[] uk2 = new byte[2];
                            readInput.Read(uk2, 0, 2);
                            input.Write(uk2, 0, 2);

                            byte[] uk3 = new byte[2];
                            readInput.Read(uk3, 0, 2);
                            input.Write(uk3, 0, 2);
                        }
                    }
                    //int cnt2 = br.ReadInt16();
                    byte[] cnt2 = new byte[2];
                    readInput.Read(cnt2, 0, 2);
                    input.Write(cnt2, 0, 2);

                    int cptShape = 0;
                    for (int i = 0; i < BitConverter.ToInt16(cnt2,0); i++)
                    {
                        byte[] id = new byte[2];
                        readInput.Read(id, 0, 2);

                        int index = _scFile.GetShapes().FindIndex(shape => shape.Id == BitConverter.ToInt16(id,0));
                        if (index != -1)
                        {
                            input.Write(BitConverter.GetBytes(_shapes[cptShape].Id), 0, 2);
                            cptShape++;
                        }
                        else
                        {
                            input.Write(id, 0, 2);
                        }
                    }
                    if (_dataType == 12)
                    {
                        for (int i = 0; i < BitConverter.ToInt16(cnt2, 0); i++)
                        {
                            input.WriteByte((byte)readInput.ReadByte());
                        }
                    }
                        
                    //read ascii
                    for (int i = 0; i < BitConverter.ToInt16(cnt2, 0); i++)
                    {
                        byte stringLength = (byte)readInput.ReadByte();
                        input.WriteByte(stringLength);
                        if (stringLength < 255)
                        {
                            for(int j=0;j<stringLength;j++)
                                input.WriteByte((byte)readInput.ReadByte());
                        }
                    }

                    byte v26;
                    while (true)
                    {
                        while (true)
                        {
                            while (true)
                            {
                                v26 = (byte)readInput.ReadByte();
                                input.WriteByte(v26);

                                //br.ReadInt32();
                                byte[] uk4 = new byte[4];
                                readInput.Read(uk4, 0, 4);
                                input.Write(uk4, 0, 4);

                                if (v26 != 5)
                                    break;
                            }
                            if (v26 == 11)
                            {
                                //short frameId = br.ReadInt16();
                                byte[] frameId = new byte[2];
                                readInput.Read(frameId, 0, 2);
                                input.Write(frameId, 0, 2);

                                byte frameNameLength = (byte)readInput.ReadByte();
                                input.WriteByte(frameNameLength);

                                if (frameNameLength < 255)
                                {
                                    for(int i=0;i<frameNameLength;i++)
                                    {
                                        input.WriteByte((byte)readInput.ReadByte());
                                    }
                                }
                            }
                            else
                                break;
                        }
                        if (v26 == 0)
                            break;
                        Debug.WriteLine("Unknown tag " + v26.ToString());
                    }        
                }
                _offset = _scFile.GetEofOffset();
                _scFile.SetEofOffset(input.Position);
                input.Write(new byte[] { 0, 0, 0, 0, 0 }, 0, 5);
            }
        }

        public void SetId(short id)
        {
            _clipId = id;
        }

        public void SetOffset(long position)
        {
            _offset = position;
        }
        #endregion
    }
}
