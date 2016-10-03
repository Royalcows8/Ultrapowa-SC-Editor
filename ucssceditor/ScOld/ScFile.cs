using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace UCSScEditor.ScOld
{
    public class ScFile
    {
        #region Constructors
        public ScFile(string fileName)
        {
            _textures = new List<ScObject>();
            _shapes = new List<ScObject>();
            _exports = new List<ScObject>();
            _movieClips = new List<ScObject>();
            _pendingChanges = new List<ScObject>();
            _fileName = fileName;
        }
        #endregion

        #region Fields & Properties
        private ushort _exportCount;
        private readonly List<ScObject> _textures;
        private readonly List<ScObject> _shapes;
        private readonly List<ScObject> _exports;
        private readonly List<ScObject> _movieClips;
        private readonly List<ScObject> _pendingChanges;

        private readonly string _fileName;
        private long _eofOffset;
        private long _exportStartOffset;
        #endregion

        public void AddChange(ScObject change)
        {
            if (_pendingChanges.IndexOf(change) == -1)
                _pendingChanges.Add(change);
        }

        public void AddExport(Export export)
        {
            _exports.Add(export);
        }

        public void AddShape(Shape shape)
        {
            _shapes.Add(shape);
        }

        public void AddTexture(Texture texture)
        {
            _textures.Add(texture);
        }

        public void AddMovieClip(MovieClip movieClip)
        {
            _movieClips.Add(movieClip);
        }

        public long GetEofOffset()
        {
            return _eofOffset;
        }

        public List<ScObject> GetExports()
        {
            return _exports;
        }

        public string GetFileName()
        {
            return _fileName;
        }

        public List<ScObject> GetMovieClips()
        {
            return _movieClips;
        }

        public List<ScObject> GetShapes()
        {
            return _shapes;
        }

        public long GetStartExportsOffset()
        {
            return _exportStartOffset;
        }

        public List<ScObject> GetTextures()
        {
            return _textures;
        }

        public void SetEofOffset(long offset)
        {
            _eofOffset = offset;
        }

        public void SetStartExportsOffset(long offset)
        {
            _exportStartOffset = offset;
        }

        public void Save(FileStream input)
        {
            // Flushing depending edits.
            List<ScObject> exports = new List<ScObject>();
            foreach (ScObject data in _pendingChanges)
            {
                if (data.GetDataType() == 7)
                    exports.Add(data);
                else
                    data.Write(input);
            }
            _pendingChanges.Clear();

            if (exports.Count > 0)
            {
                foreach (ScObject data in exports)
                {
                    data.Write(input);
                }
            }

            // Saving metadata/header.
            input.Seek(0, SeekOrigin.Begin);
            input.Write(BitConverter.GetBytes((ushort)_shapes.Count), 0, 2);
            input.Write(BitConverter.GetBytes((ushort)_movieClips.Count), 0, 2);
            input.Write(BitConverter.GetBytes((ushort)_textures.Count), 0, 2);
        }

        public void Load()
        {
            using (var texReader = new BinaryReader(File.OpenRead(_fileName.Replace(".sc", "_tex.sc"))))
            using (var reader = new BinaryReader(File.OpenRead(_fileName)))
            {
                var shapeCount = reader.ReadUInt16(); // a1 + 8
                var movieClipCount = reader.ReadUInt16(); // a1 + 12
                var textureCount = reader.ReadUInt16(); // a1 + 16
                var textFieldCount = reader.ReadUInt16(); // a1 + 24
                var matrixCount = reader.ReadUInt16(); // a1 + 28
                var colorTransformCount = reader.ReadUInt16(); // a1 + 32

                Debug.WriteLine("ShapeCount: " + shapeCount);
                Debug.WriteLine("MovieClipCount: " + movieClipCount);
                Debug.WriteLine("TextureCount: " + textureCount);
                Debug.WriteLine("TextFieldCount: " + textFieldCount);
                Debug.WriteLine("Matrix2x3Count: " + matrixCount);
                Debug.WriteLine("ColorTransformCount: " + colorTransformCount);

                // 5 useless bytes, not even used by Supercell
                reader.ReadByte(); // 1 octet
                reader.ReadUInt16(); // 2 octets
                reader.ReadUInt16(); // 2 octets

                _exportStartOffset = reader.BaseStream.Position;
                _exportCount = reader.ReadUInt16(); // a1 + 20
                Debug.WriteLine("ExportCount: " + _exportCount);

                // Reads the Export IDs.
                for (int i = 0; i < _exportCount; i++)
                {
                    var export = new Export(this);
                    export.SetId(reader.ReadInt16());
                    _exports.Add(export);
                }

                // Reads the Export names.
                for (int i = 0; i < _exportCount; i++)
                {
                    var nameLength = reader.ReadByte();
                    var name = Encoding.UTF8.GetString(reader.ReadBytes(nameLength));
                    var export = (Export)_exports[i];
                    export.SetExportName(name);
                }

                do
                {
                    long offset = reader.BaseStream.Position;
                    byte dataType = reader.ReadByte();
                    int dataLength = reader.ReadInt32();
                    switch (dataType)
                    {
                        case 0:
                            _eofOffset = offset;
                            for (int i = 0; i < _exports.Count; i++)
                            {
                                int index = _movieClips.FindIndex(movie => movie.Id == _exports[i].Id);
                                if (index != -1)
                                    ((Export)_exports[i]).SetDataObject((MovieClip)_movieClips[index]);
                            }
                            return;

                        // Textures
                        case 1:
                        case 16:
                        case 19:
                            var texture = new Texture(this);
                            texture.SetOffset(offset);
                            texture.ReadW(reader, texReader);

                            _textures.Add(texture);
                            continue;

                        // Shapes
                        case 2:
                        case 18:
                            var shape = new Shape(this);
                            shape.SetOffset(offset);
                            shape.Read(reader);
                            _shapes.Add(shape);
                            continue;

                        // Movie Clips
                        case 3:
                        case 10:
                        case 12:
                        case 14:
                            var movieClip = new MovieClip(this, dataType);
                            movieClip.SetOffset(offset);
                            movieClip.Read(reader);
                            _movieClips.Add(movieClip);
                            continue;

                        case 7:
                        case 15:
                        case 20:
                            //textFields
                            break;
                        case 8:
                            //matrix2x3
                            break;
                        case 9:
                            //colorTransform
                            break;
                        case 13:
                            break;

                        default:
                            Debug.WriteLine("Unkown data type " + dataType.ToString());
                            break;
                    }

                    // Just not to break the stream.
                    if (dataLength > 0)
                        reader.ReadBytes(dataLength);
                }
                while (true);
            }
        }
    }
}
