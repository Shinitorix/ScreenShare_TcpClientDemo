using EmemoriesDesktopViewer.Shared.Classes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace EmemoriesDesktopViewer.Server
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class ServerWindow : Window
    {
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(IntPtr value);

        private readonly List<TcpClientWithID> clientsAssociations;

        //private List<TcpClientWithID> clientsToGetDataFrom;
        //private TcpClient clientToGetDataFrom;
        private TcpListener serverToGetDataFrom;
        private int portNumberToGetDataFrom;

        //private List<TcpClientWithID> clientsToSendDataTo;
        private TcpListener serverToSendDataTo;
        private int portNumberToSendDataTo;

        //private NetworkStream mainStream;
        //private NetworkStream outStream;

        private readonly Thread listeningToGetDataFrom;
        private readonly Thread listeningToSendDataTo;

        private readonly List<Thread> listeningThreads;
        private readonly List<Thread> loopingThreads;

        public ServerWindow(int port = 98)
        {
            InitializeComponent();
            Loaded += SecondWindow_Loaded;
            Closing += SecondWindow_Closing;

            portNumberToGetDataFrom = port;
            portNumberToSendDataTo = 99;

            clientsAssociations = new List<TcpClientWithID>();

            listeningToGetDataFrom = new Thread(new ThreadStart(StartListeningToGetFrom));
            listeningToSendDataTo = new Thread(new ThreadStart(StartListeningToSendTo));

            listeningThreads = new List<Thread>();
            loopingThreads = new List<Thread>();
        }

        private void SecondWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopListening();
        }

        private void SecondWindow_Loaded(object sender, RoutedEventArgs e)
        {
            serverToGetDataFrom = new TcpListener(IPAddress.Any, portNumberToGetDataFrom);
            serverToSendDataTo = new TcpListener(IPAddress.Any, portNumberToSendDataTo);
            listeningToGetDataFrom.Start();
            listeningToSendDataTo.Start();
        }

        private void StartListeningToGetFrom()
        {
            while (true)//!clientToGetDataFrom.Connected)
            {
                serverToGetDataFrom.Start();
                TcpClient newClient = serverToGetDataFrom.AcceptTcpClient();
                ConnectNewClientToGetDataFrom(newClient);
            }
        }

        private void StartListeningToSendTo()
        {
            while (true)//!clientToSendDataTo.Connected)
            {
                serverToSendDataTo.Start();
                TcpClient newClient = serverToSendDataTo.AcceptTcpClient();

                ConnectNewClientToSendDataTo(newClient);
            }
        }

        private void StopListening()
        {
            serverToGetDataFrom.Stop();
            serverToSendDataTo.Stop();

            clientsAssociations.Clear();
            //clientsToGetDataFrom = null;
            //clientsToSendDataTo = null;

            if (listeningToGetDataFrom.IsAlive)
            {
                listeningToGetDataFrom.Abort();
            }

            if (listeningToSendDataTo.IsAlive)
            {
                listeningToSendDataTo.Abort();
            }

            //if (getImage.IsAlive)
            //{
            //    getImage.Abort();
            //}
        }

        private void ConnectNewClientToGetDataFrom(Object clientToConnectO)
        {
            TcpClient clientToConnect = (TcpClient)clientToConnectO;
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            SerializableSharedObject sso;

            bool clientRegistered = false;
            var mainStream = clientToConnect.GetStream();
            while (!clientRegistered && clientToConnect.Connected)
            {
                if (mainStream.DataAvailable)
                {
                    try
                    {
                        sso = (SerializableSharedObject)binaryFormatter.Deserialize(mainStream);
                        switch (sso.objectType)
                        {
                            case (int)SerializableObjectType.REGISTER:

                                TcpClientWithID tcpClientWithId = clientsAssociations.Where(x => x.connectionRequestId == sso.connectionRequestID).FirstOrDefault();
                                clientsAssociations.Remove(tcpClientWithId);
                                if (tcpClientWithId == null)
                                {
                                    tcpClientWithId = new TcpClientWithID(sso.connectionRequestID);
                                }
                                tcpClientWithId.customerCodeToGetDataFrom = sso.customerCode;
                                tcpClientWithId.desktopCodeToGetDataFrom = sso.desktopCode;
                                tcpClientWithId.clientToGetDataFrom = clientToConnect;
                                tcpClientWithId.streamToGetDataFrom = mainStream;
                                clientsAssociations.Add(tcpClientWithId);

                                if (tcpClientWithId.clientToGetDataFrom != null && tcpClientWithId.clientToSendDataTo != null)
                                {
                                    ReceiveResendImage(tcpClientWithId);
                                }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
        }

        private void ConnectNewClientToSendDataTo(TcpClient clientToConnect)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            SerializableSharedObject sso;

            bool clientRegistered = false;
            var mainStream = clientToConnect.GetStream();
            while (!clientRegistered && clientToConnect.Connected)
            {
                if (mainStream.DataAvailable)
                {
                    sso = (SerializableSharedObject)binaryFormatter.Deserialize(mainStream);
                    switch (sso.objectType)
                    {
                        case (int)SerializableObjectType.REGISTER:
                            TcpClientWithID tcpClientWithId = clientsAssociations.Where(x => x.connectionRequestId == sso.connectionRequestID).FirstOrDefault();
                            clientsAssociations.Remove(tcpClientWithId);
                            if (tcpClientWithId == null)
                            {
                                tcpClientWithId = new TcpClientWithID(sso.connectionRequestID);
                            }
                            tcpClientWithId.customerCodeToSendDataTo = sso.customerCode;
                            tcpClientWithId.desktopCodeToSendDataTo = sso.desktopCode;
                            tcpClientWithId.clientToSendDataTo = clientToConnect;
                            tcpClientWithId.streamToSendDataTo = mainStream;
                            clientsAssociations.Add(tcpClientWithId);

                            if (tcpClientWithId.clientToGetDataFrom != null && tcpClientWithId.clientToSendDataTo != null)
                            {
                                ReceiveResendImage(tcpClientWithId);
                            }
                            break;
                    }
                }
            }
        }

        private void ReceiveResendImage(TcpClientWithID clientAssociation)
        {
            if (clientAssociation != null)
            {
                clientAssociation.isListening = true;
            }
            else
                return;

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            SerializableSharedObject sso = new SerializableSharedObject(1);
            if (clientAssociation.clientToSendDataTo.Connected && clientAssociation.clientToGetDataFrom.Connected)
            {
                // avviso chi invia i dati che siamo pronti per ricevere.
                clientAssociation.streamToGetDataFrom.Write(new byte[1] { (byte)1 }, 0, 1);

                while (clientAssociation.clientToSendDataTo.Connected && clientAssociation.clientToGetDataFrom.Connected)
                {
                    byte[] readMsgLen = new byte[4];
                    while (!clientAssociation.streamToGetDataFrom.DataAvailable)
                    {
                        // aspetto... 
                    }
                    clientAssociation.streamToGetDataFrom.Read(readMsgLen, 0, 4);
                    int dataLen = BitConverter.ToInt32(readMsgLen, 0);

                    byte[] readMsgData = new byte[dataLen];

                    //invio al client un feedback di messaggio ricevuto.
                    clientAssociation.streamToGetDataFrom.Write(new byte[1] { (byte)1 }, 0, 1);

                    while (!clientAssociation.streamToGetDataFrom.DataAvailable)
                    {
                        // aspetto... 
                    }

                    clientAssociation.streamToGetDataFrom.Read(readMsgData, 0, dataLen);

                    //if (clientAssociation.streamToGetDataFrom.DataAvailable)
                    //{
                    //byte[] readMsgLen = new byte[4];
                    //clientAssociation.streamToGetDataFrom.Read(readMsgLen, 0, 4);

                    ////invio al client un feedback di messaggio ricevuto.
                    //clientAssociation.streamToGetDataFrom.Write(new byte[1] { (byte)1 }, 0, 1);

                    //int dataLen = BitConverter.ToInt32(readMsgLen, 0);
                    //byte[] readMsgData = new byte[dataLen];
                    //clientAssociation.streamToGetDataFrom.Read(readMsgData, 0, dataLen);


                    try
                    {
                        sso = ByteArrayToSerializableSharedObject<SerializableSharedObject>(readMsgData);
                        switch (sso.objectType)
                        {
                            case (int)SerializableObjectType.SCREEN:
                                //byte[] ba = ObjectToByteArray(sso);
                                //byte[] userDataLen = BitConverter.GetBytes((Int32)ba.Length);
                                //clientAssociation.streamToSendDataTo.Write(userDataLen, 0, 4);
                                //clientAssociation.streamToSendDataTo.Write(ba, 0, ba.Length);

                                ////rimango in attesa che il client mi dica di aver ricevuto la foto.
                                //bool messageSent = false;
                                //while (!messageSent)
                                //{
                                //    if (clientAssociation.streamToSendDataTo.DataAvailable)
                                //    {
                                //        byte[] sendResult = new byte[1];
                                //        if (clientAssociation.streamToSendDataTo.Read(sendResult, 0, 1) > 0)
                                //        {
                                //            if (sendResult[0] == (byte)1)
                                //                messageSent = true;
                                //        }
                                //    }
                                //}

                                break;
                            default:
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    //invio al client un feedback di messaggio ricevuto.
                    clientAssociation.streamToGetDataFrom.Write(new byte[1] { (byte)1 }, 0, 1);
                }
            }
            clientAssociation.isListening = false;
            clientAssociation.streamToSendDataTo.Dispose();
            clientAssociation.streamToGetDataFrom.Dispose();
            clientsAssociations.Remove(clientAssociation);
        }

        byte[] ObjectToByteArray(object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }
        private T ByteArrayToSerializableSharedObject<T>(byte[] arrBytes)
        {
            T obj;

            using (MemoryStream memStream = new MemoryStream())
            {
                BinaryFormatter binForm = new BinaryFormatter();
                memStream.Write(arrBytes, 0, arrBytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                obj = (T)binForm.Deserialize(memStream);
            }
            return obj;
        }

    }
}
