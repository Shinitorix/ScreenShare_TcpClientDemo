using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EmemoriesDesktopViewer.Shared.Classes
{
    public class TcpClientWithID
    {
        private Object _lock = new Object();

        private bool _isListening;
        public bool isListening
        {
            get
            {

                lock (_lock)
                {
                    return _isListening;
                }
            }
            set
            {
                lock (_lock)
                {
                    _isListening = value;
                }
            }
        }

        public TcpClientWithID(int connectionRequestId)
        {
            this.connectionRequestId = connectionRequestId;
        }

        public TcpClient clientToGetDataFrom { get; set; }
        public NetworkStream streamToGetDataFrom { get; set; }
        public TcpClient clientToSendDataTo { get; set; }
        public NetworkStream streamToSendDataTo { get; set; }

        public int connectionRequestId { get; set; }

        public string customerCodeToSendDataTo { get; set; }
        public string desktopCodeToSendDataTo { get; set; }

        public string customerCodeToGetDataFrom { get; set; }
        public string desktopCodeToGetDataFrom { get; set; }
    }
}
