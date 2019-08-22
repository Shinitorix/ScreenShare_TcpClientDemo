using EmemoriesDesktopViewer.Shared.Classes;
using Newtonsoft.Json;
using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace EmemoriesDesktopViewer.Client
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class ShareScreenWindow : Window
    {
        private int connectionIdTest = 1000;

        private bool sharingScreen = false;

        private readonly TcpClient client = new TcpClient();
        private int portNumber;

        //private System.Windows.Forms.Timer mTimer;

        public ShareScreenWindow()
        {
            InitializeComponent();
            //mTimer = new System.Windows.Forms.Timer();
            //mTimer.Interval = 100;
            //mTimer.Tick += MTimer_Tick;
        }

        //private void MTimer_Tick(object sender, System.EventArgs e)
        //{
        //    SendDesktopImage();
        //}

        bool readyToSend = false;

        private void SendDesktopImage(NetworkStream clientStream)
        {

            if (client.Connected && readyToSend)
            {
                //using (var mainStream = client.GetStream())
                //{
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                while (client.Connected)
                {
                    SerializableSharedObject sso = new SerializableSharedObject(connectionIdTest)
                    {
                        timeStamp = DateTime.Now,
                        customerCode = "TEST",
                        desktopCode = "DESK",
                        objectType = (int)SerializableObjectType.SCREEN,
                        screen = GrabDesktop()
                    };
                    byte[] ba = ObjectToByteArray(sso);
                    byte[] userDataLen = BitConverter.GetBytes((Int32)ba.Length);
                    clientStream.Write(userDataLen, 0, 4);

                    bool lengthSent = false;
                    while (!lengthSent)
                    {
                        if (clientStream.DataAvailable)
                        {
                            byte[] sendResult = new byte[1];
                            if (clientStream.Read(sendResult, 0, 1) > 0)
                            {
                                if (sendResult[0] == (byte)1)
                                    lengthSent = true;
                            }
                        }
                    }

                    clientStream.Write(ba, 0, ba.Length);

                    bool contentSent = false;
                    while (!contentSent)
                    {
                        if (clientStream.DataAvailable)
                        {
                            byte[] sendResult = new byte[1];
                            if (clientStream.Read(sendResult, 0, 1) > 0)
                            {
                                if (sendResult[0] == (byte)1)
                                    contentSent = true;
                            }
                        }
                    }
                    //binaryFormatter.Serialize(mainStream, sso);
                    //}
                }
            }
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

        private void BtnShare_Click(object sender, RoutedEventArgs e)
        {
            if (!sharingScreen)
            {
                new Thread(() =>
                {
                    using (NetworkStream clientStream = client.GetStream())
                    {
                        while (!readyToSend)
                        {
                            if (clientStream.DataAvailable)
                            {
                                byte[] sendResult = new byte[1];
                                if (clientStream.Read(sendResult, 0, 1) > 0)
                                {
                                    if (sendResult[0] == (byte)1)
                                        readyToSend = true;
                                }
                            }
                        }
                        SendDesktopImage(clientStream);
                    }
                    sharingScreen = true;
                }).Start();
                btnShare.Content = "Interrompi condivisione";
                btnConnect.Visibility = Visibility.Hidden;
            }
            else
            {
                //mTimer.Stop();
                sharingScreen = false;
                btnShare.Content = "Condividi lo schermo";
                btnConnect.Visibility = Visibility.Visible;
                client.Dispose();
                //connected = false;
            }
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            portNumber = int.Parse(tbPortTo.Text);
            try
            {
                //client.Connect(tbIpTo.Text, portNumber);
                client.Connect("127.0.0.1", portNumber);
                //connected = true;

                registerClient(client);
            }
            catch (System.Exception)
            {
                //connected = false;
                System.Windows.Forms.MessageBox.Show("not Connected!");
                throw;
            }
        }

        private void registerClient(TcpClient client)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            var mainStream = client.GetStream();
            SerializableSharedObject sso = new SerializableSharedObject(connectionIdTest)
            {
                timeStamp = DateTime.Now,
                customerCode = "TEST",
                desktopCode = "DESK",
                objectType = (int)SerializableObjectType.REGISTER,
                screen = null
            };
            binaryFormatter.Serialize(mainStream, sso);
        }

        private static System.Drawing.Bitmap GrabDesktop()
        {
            Rectangle bounds = Screen.GetBounds(System.Drawing.Point.Empty);
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(System.Drawing.Point.Empty, System.Drawing.Point.Empty, bounds.Size);
            }

            return bitmap;
            //Rectangle bounds = Screen.PrimaryScreen.Bounds;
            //Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height);
            //Graphics graphics = Graphics.FromImage(screenshot);
            //graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            //return screenshot;
        }
    }
}
