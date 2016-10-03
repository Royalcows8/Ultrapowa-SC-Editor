using System.Collections.Generic;
using System.IO;
using System.Drawing;

namespace UCSScEditor
{
    public class ScObject
    {
        #region Constructor
        public ScObject()
        {
            // Space
        }
        #endregion

        #region Fields & Properties
        public virtual Bitmap Bitmap => null;

        public virtual List<ScObject> Children => new List<ScObject>();
        #endregion

        #region Methods
        public virtual int GetDataType()
        {
            return -1;
        }

        public virtual string GetDataTypeName()
        {
            return null;
        }

        public virtual short GetId()
        {
            return -1;
        }

        public virtual string GetInfo()
        {
            return string.Empty;
        }

        public virtual string GetName()
        {
            return GetId().ToString();
        }

        public virtual bool IsImage()
        {
            return false;
        }

        public virtual Bitmap Render(RenderingOptions options) => null;

        public virtual void ParseData(BinaryReader br)
        {
            // Space
        }

        public virtual void Save(FileStream input)
        {
            // Space
        }
        #endregion
    }
}
