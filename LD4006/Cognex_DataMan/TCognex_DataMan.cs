using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.ComponentModel;

using Cognex.DataMan.SDK;
using System.Threading;
using Cognex.DataMan.SDK.Discovery;
using System.Net.Sockets;
using Cognex.DataMan.SDK.Utils;
//using ICSharpCode.SharpZipLib.Zip;
using EFC.Tool;


namespace Cognex.DataMan
{
    public class TReader_Cognex_DataMan
    {
        public TLog      Log = null;
        public Image     Read_Image = null;
        public bool      Reflash = false;
        public ArrayList Device_List = new ArrayList();
        private Socket ConnectionSocket;
        private SynchronizationContext _syncContext = new SynchronizationContext();
        private DataManSystem System = null;
        private EthSystemDiscoverer System_Discoverer = null;
        private EthSystemDiscoverer.SystemInfo System_Info;
        //private EthSystemConnector Connector = null;
        private ResultCollector Result_Collector = null;
        private ISystemConnector _connector = null;

        private bool InConnect = false;
        private bool in_On_Life = false;
        private string Read_String = "";
        private bool Read_Finish = false;


        public bool Connect
        {
            get
            {
                return InConnect;
            }
            set
            {
                Set_Connect(value);
            }
        }
        public bool On_Life
        {
            get
            {
                return in_On_Life;
            }
            set
            {
                Set_Life(value);
            }
        }
        public TReader_Cognex_DataMan()
        {
            try
            {
                // Create discoverers to discover ethernet and serial port systems.
                System_Discoverer = new EthSystemDiscoverer();

                // Subscribe to the system discoved event.
                System_Discoverer.SystemDiscovered += new EthSystemDiscoverer.SystemDiscoveredHandler(OnEthSystemDiscovered);

                // Ask the discoverers to start discovering systems.
                System_Discoverer.Discover();
                //System.Threading.Thread.Sleep(5000);
            }
            catch (Exception e)
            {

            }
        }

        //**********************列出所連線到的DM************************************
        private void OnEthSystemDiscovered(EthSystemDiscoverer.SystemInfo systemInfo)
        {

            System_Info = systemInfo;
            Device_List.Add(systemInfo);
            //lbCameraName.Text = systemInfo.Name;
            //lbIP.Text = Convert.ToString(systemInfo.IPAddress);
            //_syncContext.Post(
            //   new SendOrPostCallback(
            //       delegate
            //       {
            //           Device_List.Add(systemInfo.ToString());
            //           eth_system_info = systemInfo;
            //       }),
            //       null);
        }
        public void Set_Connect(bool state)
        {
          
            if (Device_List.Count > 0)
            {
                try
                {
                 
                    EthSystemDiscoverer.SystemInfo eth_system_info = (EthSystemDiscoverer.SystemInfo)Device_List[0];
                    EthSystemConnector conn = new EthSystemConnector(eth_system_info.IPAddress, eth_system_info.Port);


                    IPAddress ipa = eth_system_info.IPAddress;
                    //Connector = new EthSystemConnector(ipa, 4001);
                    
                    conn.UserName = "admin";
                    conn.Password = "";

                    _connector = conn;

                    ConnectionSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    ConnectionSocket.Connect(eth_system_info.IPAddress, eth_system_info.Port);       

                    System = new DataManSystem(_connector);
                    System.DefaultTimeout = 5000;

                    System.SystemConnected += new SystemConnectedHandler(OnSystemConnected);
                    //System.SystemDisconnected += new SystemDisconnectedHandler(OnSystemDisconnected);

                    //System.SystemWentOnline += new SystemWentOnlineHandler(OnSystemWentOnline);
                    //System.SystemWentOffline += new SystemWentOfflineHandler(OnSystemWentOffline);
                    //System.KeepAliveResponseMissed += new KeepAliveResponseMissedHandler(OnKeepAliveResponseMissed);
                    //System.BinaryDataTransferProgress += new BinaryDataTransferProgressHandler(OnBinaryDataTransferProgress);
                    //System.TrainingResultArrived += new TrainingResultArrivedHandler(OnTrainingResultArrived);
                    ResultTypes requested_result_types = ResultTypes.ReadXml | ResultTypes.Image | ResultTypes.ImageGraphics;


                    Result_Collector = new ResultCollector(System, requested_result_types);
                  //  Result_Collector.ComplexResultArrived += Results_ComplexResultArrived;
                    System.XmlResultArrived += new XmlResultArrivedHandler(OnXmlResultArrived);
                   // Result_Collector.ComplexResultCompleted += Results_ComplexResultArrived;
                 //   Result_Collector.SimpleResultDropped += Results_SimpleResultDropped;
                    System.Connect();
                    

                    System.SetResultTypes(requested_result_types);
                    if (System.State == Cognex.DataMan.SDK.ConnectionState.Connected)
                    {
                        InConnect = true;
                        //BtnTrigger.Enabled = true;
                        //cbLiveDisplay.Enabled = true;

                        //btnConnect.BackColor = Color.Red;
                        //btnConnect.Text = "Disconnection";

                        //取得DM值
                        //DM_data_get();
                    }
                    else
                    {

                    }
                }
                catch
                {
                }
            }
        }
        private void OnXmlResultArrived(object sender, XmlResultArrivedEventArgs args)
        {

            _syncContext.Post(
                delegate
                {
                    //   MessageBox.Show(args.ToString());
                    //List<string> dropped = new List<string>();
                    //if (e.Image != null) dropped.Add(String.Format("image (ResultId={0}, ImageId={1})", e.ResultId, e.ImageId));
                    //if (e.ImageGraphics != null) dropped.Add(String.Format("graphics (ResultId={0}, ImageId={1})", e.ResultId, e.ImageId));
                    //if (e.ReadString != null) dropped.Add(String.Format("read string (ResultId={0})", e.ResultId));
                    //if (e.XmlResult != null) dropped.Add(String.Format("xml result (ResultId={0})", e.ResultId));
                    //  AddListItem("Partial results dropped: " + args.XmlResult); //String.Join(", ", dropped.ToArray()));


                    //picResultImage.Image = null;
                   // picResultImage.BackColor = Color.Blue;
                    Image img = null;
                    Read_String=  Get_String_Xml(args.XmlResult.ToString());
                    ResultInfo mResultinfo = new ResultInfo(args.ResultId, args.ResultId, img, "", "", args.XmlResult);
                   

                   // Read_String = args.XmlResult.ToString();
                    //   OnResultInQueue.Enqueue(mResultinfo);
                  //  tbLog.Text = args.XmlResult.ToString();
                }, null);

        }
        void Results_ComplexResultArrived(object sender, ComplexResult e)
        {

            _syncContext.Post(
                delegate
                {
                    // MessageBox.Show(e.Image.Height.ToString());    
                    //if (TriggerCount == 0 || (TriggerCount - OnTriggerCount <= 0))
                    //{
                    //    timer_Cycle_Run.Stop();
                    //    btn_ContinuousRun.Text = btn_ContinuousRun.Text = "Continuous";
                    //}
                    //else
                    //{
                    //   // _system.SendCommand("TRIGGER ON");
                    //    ++OnTriggerCount;
                    //    txtOnTriggerCount.Text = OnTriggerCount.ToString();
                    //}               
                    //ShowResult(e);

                 //   picResultImage.BackColor = Color.White;
                    //OnResultInQueue.Enqueue(e);

                    ShowResult(e);
                },
                null);
        }
        private void OnSystemConnected(object sender, EventArgs args)
        {
            _syncContext.Post(
                delegate
                {
                    AddListItem("System connected");
                    RefreshGui();
                },
                null);
        }
        private void Results_SimpleResultDropped(object sender, SimpleResult e)
        {
            Read_String = Get_String_Xml(e.GetDataAsString());
            //_syncContext.Post(
            //    delegate
            //    {
            //        ReportDroppedResult(e);
            //    },
            //    null);
        }
        private void ReportDroppedResult(SimpleResult result)
        {
            AddListItem(string.Format("Partial result dropped: {0}, id={1}", result.Id.Type.ToString(), result.Id.Id));
            Read_String = Get_String_Xml(result.GetDataAsString());
        }
        private void RefreshGui()
        {
            //bool system_connected = _system != null && _system.State == ConnectionState.Connected;
            //bool system_ready_to_connect = _system == null || _system.State == ConnectionState.Disconnected;
            //bool gui_ready_to_connect = listBoxDetectedSystems.SelectedIndex != -1 && listBoxDetectedSystems.Items.Count > listBoxDetectedSystems.SelectedIndex;

            //btnConnect.Enabled = system_ready_to_connect && gui_ready_to_connect;
            //btnDisconnect.Enabled = system_connected;
            //btnTrigger.Enabled = system_connected;
            //cbLiveDisplay.Enabled = system_connected;
        }
        private void Results_ComplexResultCompleted(object sender, ComplexResult e)
        {
            _syncContext.Post(
                delegate
                {
                    ShowResult(e);
                },
                null);
        }
        private void ShowResult(ComplexResult e)
        {
            try
            {
                List<Image> Show_images = new List<Image>();
                List<string> Show_image_graphics = new List<string>();
                List<string> ReadResults = new List<string>();
                string read_result = null;
                int result_id = -1;
                ResultTypes collected_results = ResultTypes.None;
                //read_result = !String.IsNullOrEmpty(e.ReadString) ? e.ReadString : GetReadStringFromResultXml(e.XmlResult);

                //    lock (_currentResultInfoSyncLock)
                {
                    foreach (var simple_result in e.SimpleResults)
                    {
                        collected_results |= simple_result.Id.Type;

                        switch (simple_result.Id.Type)
                        {
                            case ResultTypes.Image:
                                Image image = ImageArrivedEventArgs.GetImageFromImageBytes(simple_result.Data);
                                if (image != null)
                                    Show_images.Add(image);
                                break;

                            case ResultTypes.ImageGraphics:
                                Show_image_graphics.Add(simple_result.GetDataAsString());
                                break;

                            case ResultTypes.ReadXml:
                                read_result = Get_String_Xml(simple_result.GetDataAsString());//GetReadStringFromResultXml(simple_result.GetDataAsString());
                                result_id = simple_result.Id.Id;
                                break;

                            case ResultTypes.ReadString:
                                read_result = simple_result.GetDataAsString();
                                result_id = simple_result.Id.Id;
                                break;
                        }
                    }
                }

                //                AddListItem(string.Format("Complex result arrived: resultId = {0}, read result = {1}", result_id, read_result));
                // Log("Complex result contains", string.Format("{0}", collected_results.ToString()));


                //  ShowImage(e);


                //if (Show_images.Count > 0)
                //{
                //    Image first_image = Show_images[0];

                //    Size image_size = Gui.FitImageInControl(first_image.Size, picResultImage.Size);
                //    Image fitted_image = Gui.ResizeImageToBitmap(first_image, image_size);

                //    if (Show_image_graphics.Count > 0)
                //    {
                //        using (Graphics g = Graphics.FromImage(fitted_image))
                //        {
                //            foreach (var graphics in Show_image_graphics)
                //            {
                //                ResultGraphics rg = GraphicsResultParser.Parse(graphics, new Rectangle(0, 0, image_size.Width, image_size.Height));
                //                ResultGraphicsRenderer.PaintResults(g, rg);
                //            }
                //        }
                //    }

                //    if (picResultImage.Image != null)
                //    {
                //        var image = picResultImage.Image;
                //        picResultImage.Image = null;
                //        image.Dispose();
                //    }

                //    //picResultImage.Image = fitted_image;
                //    //picResultImage.Invalidate();

                //    picResultImage.Image = fitted_image;
                //    //OnResults_Queue.Enqueue(read_result);
                //    //SaveImage_Queue.Enqueue(first_image);
                //    SaveImage(first_image, read_result);
                //    picResultImage.Invalidate();
            }


            ////List<Image> Show_images = new List<Image>();
            ////List<string> Show_image_graphics = new List<string>();
            //List<string> ReadResults = new List<string>();
            //string read_result = null;
            //// Take a reference or copy values from the locked result info object. This is done
            //// so that the lock is used only for a short period of time.
            ////lock (_currentResultInfoSyncLock)
            ////{

            //    read_result = !String.IsNullOrEmpty(e.ReadString) ? e.ReadString : GetReadStringFromResultXml(e.XmlResult);
            //    GetResult(ReadResults, e.XmlResult);  
            //    tbLog.Text = e.XmlResult;
            //    AddListItem("Complex result arrived : resultId = " + e.ResultId + ", read result = " + read_result);





            //   ShowImage(e);



            //if (read_result != null)
            //{
            //    string[] str = new string[2];
            //    string TriggerTime = "";
            //    string DecodeTime = "0";
            //    string mStrlog = "0";
            //    foreach (string strlog in ReadResults)
            //    {
            //        str = strlog.Split(':');
            //        if (str[0] == "trigger_time")
            //        {
            //            TriggerTime = strlog;
            //        }
            //        if (str[0] == "decode_time")
            //        {
            //            DecodeTime = strlog;
            //        }
            //        string temlog = "<" + strlog + ">";
            //        mStrlog = mStrlog + temlog + ";";
            //    }
            //        lbReadString.Text = "<ReadResult>" + read_result.Trim() + ";" + TriggerTime + ";" + DecodeTime;
            //        SaveLog(mStrlog, "");
            //    }
            //}
            //// }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message.ToString());
            //    listBox1.Items.Add(ex.Message.ToString());
            }
        }
        

        private void AddListItem(object item)
        {
            //lock (_listAddItemLock)
            //{
            //    listBox1.Items.Add(item);

            //    if (listBox1.Items.Count > 500)
            //        listBox1.Items.RemoveAt(0);

            //    if (listBox1.Items.Count > 0)
            //        listBox1.SelectedIndex = listBox1.Items.Count - 1;
            //}
        }
        public void Set_Life(bool state)
        {
            try
            {
                in_On_Life = state;
                if (in_On_Life)
                {
                    System.SendCommand("SET LIVEIMG.MODE 2");
                    BeginGetLiveImage();
                }
                else
                {
                    System.SendCommand("SET LIVEIMG.MODE 0");
                }
            }
            catch
            {
            }
        }
        public bool Get_Code(ref string read_code)
        {
            bool result = false;

            if (Connect && !On_Life)
            {
                Read_Finish = false;
             //   Read_String = "";
                Trigger_ON();
               // Result_Collector.SimpleResultDropped += Results_SimpleResultDropped;
                 
              //  Trigger_OFF();
              while (!Read_Finish) { };
                read_code = Read_String;             
                read_code = read_code.Replace("\r", "");
                read_code = read_code.Replace("\n", "");
                Log_Add(string.Format("Get_Code = {0:s}", Read_String));
                result = true;
            }
            else
            {
                Log_Add(string.Format("Get_Code Error."));
            }
            return result;
        }


        public void Log_Add(string msg)
        {
            if (Log != null && Log.Enabled) Log.Add(msg);
        }

        private void Trigger_ON()
        {
            try
            {
                System.SendCommand("TRIGGER ON");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to send TRIGGER ON command: " + ex.ToString());
            }
        }
        private void Trigger_OFF()
        {
            try
            {
                System.SendCommand("TRIGGER OFF");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to send TRIGGER OFF command: " + ex.ToString());
            }
        }
       public void Results_ComplexResultArrived(object sender, ResultInfo e)
        {
            if (e.Image != null)
            {
                Read_Image = (Image)e.Image.Clone();
                Reflash = true;
            }
            if (e.ReadString != null)
            {
                Read_String = e.ReadString;
            }
            else
            {
                Read_String = Get_String_Xml(e.XmlResult);
            }
            Read_Finish = true;
        }
        private string Get_String_Xml(string resultXml)
        {
            string result = "";
            try
            {
                XmlDocument doc = new XmlDocument();

                doc.LoadXml(resultXml);

                XmlNode full_string_node = doc.SelectSingleNode("result/general/full_string");

                if (full_string_node != null)
                {
                    XmlAttribute encoding = full_string_node.Attributes["encoding"];
                    if (encoding != null && encoding.InnerText == "base64")
                    {
                        byte[] code = Convert.FromBase64String(full_string_node.InnerText);
                        result = System.Encoding.GetString(code, 0, code.Length);
                    }
                    else
                    {
                        result = full_string_node.InnerText;
                    }
                }
            }
            catch
            {
            }

            return result;
        }
        private void BeginGetLiveImage()
        {
            System.BeginGetLiveImage(
                Cognex.DataMan.SDK.ImageFormat.jpeg,
                ImageSize.Full,
                ImageQuality.High,
                OnLiveImageArrived,
                null);
        }
        private void OnLiveImageArrived(IAsyncResult result)
        {
            try
            {
                Read_Image = System.EndGetLiveImage(result);
                Reflash = true;
            }
            catch
            {
            }
            finally
            {
                if (in_On_Life) BeginGetLiveImage();
            }
        }

    }
}
