using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace EmemoriesDesktopViewer.Shared.Classes
{
    [Serializable()]
    public class SerializableSharedObject : ISerializable
    {
        public DateTime timeStamp { get; set; }
        public string customerCode { get; set; }
        public string desktopCode { get; set; }

        public int connectionRequestID { get; set; }

        public int objectType { get; set; }
        public System.Drawing.Bitmap screen { get; set; }
        //public byte[] screen { get; set; }


        public SerializableSharedObject(int connectionID)
        {
            this.connectionRequestID = connectionID;
        }

        public SerializableSharedObject() { }

        public SerializableSharedObject(SerializationInfo info, StreamingContext context)
        {
            timeStamp = info.GetDateTime("timeStamp");
            customerCode = info.GetString("customerCode");
            desktopCode = info.GetString("desktopCode");
            connectionRequestID = info.GetInt32("connectionRequestID");
            objectType = info.GetInt32("objectType");
            screen = (System.Drawing.Bitmap)info.GetValue("screen", typeof(System.Drawing.Bitmap));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("timeStamp", timeStamp);
            info.AddValue("customerCode", customerCode);
            info.AddValue("desktopCode", desktopCode);
            info.AddValue("connectionRequestID", connectionRequestID);
            info.AddValue("objectType", objectType);
            info.AddValue("screen", screen);
        }
    }
}
