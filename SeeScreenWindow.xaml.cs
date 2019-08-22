using EmemoriesDesktopViewer.Shared.Classes;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Newtonsoft.Json;

namespace EmemoriesDesktopViewer.Client
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class SeeScreenWindow : Window
    {
        private int connectionIdTest = 1000;


        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(IntPtr value);

        private readonly TcpClient client = new TcpClient();

        private int portNumber = 99;

        private readonly Thread listening;
        //private readonly Thread getImage;


        public SeeScreenWindow()
        {
            InitializeComponent();
            Loaded += SecondWindow_Loaded;
            Closing += SecondWindow_Closing;

            listening = new Thread(new ThreadStart(StartListening));
            //getImage = new Thread(new ParameterizedThreadStart(ReceiveImage));

        }

        private void SecondWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopListening();
        }

        private void SecondWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //server = new TcpListener(IPAddress.Any, portNumber);
            listening.Start();
        }

        private void StartListening()
        {
            if (!client.Connected)
            {
                client.Connect("127.0.0.1", 99);
            }

            using (NetworkStream mainStream = client.GetStream())
            {
                registerClient(client, mainStream);
                ReceiveImage(mainStream);
            }
        }

        private void StopListening()
        {
            //server.Stop();
            client.Dispose();
            if (listening.IsAlive)
            {
                listening.Abort();
            }
            //if (getImage.IsAlive)
            //{
            //    getImage.Abort();
            //}
        }

        private void ReceiveImage(object mainStream)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            SerializableSharedObject sso;
            //using (NetworkStream mainStream = client.GetStream())
            //{
            while (client.Connected)
            {
                if (((NetworkStream)mainStream).DataAvailable)
                {
                    byte[] readMsgLen = new byte[4];
                    ((NetworkStream)mainStream).Read(readMsgLen, 0, 4);

                    int dataLen = BitConverter.ToInt32(readMsgLen, 0);
                    byte[] readMsgData = new byte[dataLen];
                    ((NetworkStream)mainStream).Read(readMsgData, 0, dataLen);

                    try
                    {
                        sso = ByteArrayToSerializableSharedObject(readMsgData);
                        switch (sso.objectType)
                        {
                            case (int)SerializableObjectType.SCREEN:
                                IntPtr bmpPt = sso.screen.GetHbitmap();
                                BitmapSource bitmapSource =
                                     System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                           bmpPt,
                                           IntPtr.Zero,
                                           Int32Rect.Empty,
                                           BitmapSizeOptions.FromEmptyOptions());

                                // freeze bitmapSource and clear memory to avoid memory leaks
                                if (bitmapSource.CanFreeze && !bitmapSource.IsFrozen)
                                    bitmapSource.Freeze();
                                DeleteObject(bmpPt);

                                if (displayImage.Dispatcher.CheckAccess())
                                {
                                    displayImage.Source = bitmapSource;
                                }
                                else
                                {
                                    Action act = () => { displayImage.Source = bitmapSource; };
                                    displayImage.Dispatcher.BeginInvoke(act);
                                }

                                // invio al server un messaggio di foto ricevuta.
                                ((NetworkStream)mainStream).Write(new byte[1] { (byte)1 }, 0, 1);

                                break;
                            default:
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
                //}
            }
        }
        private SerializableSharedObject ByteArrayToSerializableSharedObject(byte[] arrBytes)
        {
            SerializableSharedObject obj = null;

            using (MemoryStream memStream = new MemoryStream())
            {
                BinaryFormatter binForm = new BinaryFormatter();
                memStream.Write(arrBytes, 0, arrBytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                obj = (SerializableSharedObject)binForm.Deserialize(memStream);
            }
            return obj;
        }

        private void registerClient(TcpClient client, NetworkStream mainStream)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            SerializableSharedObject sso = new SerializableSharedObject(connectionIdTest)
            {
                timeStamp = DateTime.Now,
                customerCode = "ADMIN",
                desktopCode = "ADMIN_DESK",
                objectType = (int)SerializableObjectType.REGISTER
            };
            binaryFormatter.Serialize(mainStream, sso);
        }
    }
}
