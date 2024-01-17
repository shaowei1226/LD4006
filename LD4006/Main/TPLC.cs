using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EFC.Tool;
using EFC.PLC;
using EFC.PLC.Melsec;//TMelsec_QPLC_Eth_Connect功能

namespace Main
{
    public delegate void evPLC_No_Thread_Run(string name);
    public class TPLC
    {
        public TMelsec_QPLC_Eth_Connect          PLC_Socket = new TMelsec_QPLC_Eth_Connect();      
        public TLog                              in_Log = null;
        public TPLC_Data_In                      PLC_In = new TPLC_Data_In();
        public TPLC_Data_Out                     PLC_Out = new TPLC_Data_Out();
        public TPLC_Data_Recipe                  PLC_Recipe = new TPLC_Data_Recipe();
        private Thread                           PLC_Thread;
        private PLC_Thread_List                  Thread_List = new PLC_Thread_List();
        static public int                        alarm_tmp_count = 10;
        public String[,]                         Alarm_Message_list = new String[alarm_tmp_count, 16];
        public bool                              Update_Recipe_Flag = false;
        public bool                              MU_Right_OK = false;
        public bool                              MU_Left_OK = false;
        public string                            Log_Source = "TPLC";
                                               
                                                

        private bool                             Terminate = false;
        private bool                             Thread_ON = false;
        private double                           in_Scan_Time;
        private System.Diagnostics.Stopwatch     Watch = new System.Diagnostics.Stopwatch();


        public TPLC()
        {
            
            PLC_Thread = new Thread(Thread_Start); }
        public void Dispose()
        {
            Stop();
        }
        public TLog Log
        {
            get
            { 
                return in_Log; 
            }
            set
            {
                in_Log = value; 
                //PLC_Socket.Log = value; 
            }
        }
        public double Scan_Time
        {
            get
            {
                return in_Scan_Time;
            }
        }
        public void Log_Add(string fun, string msg_str, emLog_Type type = emLog_Type.Generally)
        {
            if (Log != null) Log.Add(Log_Source, fun, msg_str, type);
        }
        public void Start()
        {
            PLC_Thread.Start();
        }
        public void Stop()
        {
            Terminate = true;
            while (Thread_ON)
            {
                Application.DoEvents();
            }
        }
        public void Thread_Start()
        {
            while (!Terminate)
            {
                Thread_ON = true;
                Watch.Reset();
                Watch.Start();

                PLC_Out.On_Line = !PLC_Out.On_Line;

                Read_From_PLC();
                Write_To_PLC();
           //     Run_Fun(PLC_In.Write_Recipe_Req, PLC_Out.Write_Recipe, "Write_Recipe", Write_Recipe);
                No_Thread_Add(PLC_In.Write_Recipe_Req, PLC_Out.Write_Recipe, "Write_Recipe", Write_Recipe);
             
                Thread.Sleep(100);
                Watch.Stop();
                in_Scan_Time = Watch.Elapsed.TotalMilliseconds;
          
            }
            Thread_ON = false;
        }
        private void No_Thread_Add(bool req, TPLC_CMD_Data cmd, string name, evPLC_No_Thread_Run run_fun)
        {
            if (req && !cmd.Running && !cmd.Finish)
            {
                cmd.Running = true;
                run_fun(name);
            }
            else if (!req)
            {
                cmd.Running = false;
                cmd.Finish = false;
                cmd.OK = false;
            }
        }
        private void Run_Fun(bool req, TPLC_CMD_Data cmd, string name, evPLC_Thread_Run run_fun)
        {
            if (req && !cmd.Running && !cmd.Finish)
            {
                cmd.Running = true;
                Log_Add(name, string.Format("[PLC] Thread Name={0:s}", name));
                run_fun(name);
            }
            else if (!req)
            {
                cmd.Running = false;
                cmd.Finish = false;
                cmd.OK = false;
            }
        }


        private void Read_From_PLC()
        {
            string fun = "Read_From_PLC";

            if (PLC_Socket.Connect)
            {
                if (!PLC_In.Read(PLC_Socket))
                {
                    Log_Add(fun, "[PLC] Read_From_PLC Error.", emLog_Type.Error);
                    JJS_LIB.Sleep(2000);
                }
            }
        }
        private void Write_To_PLC()
        {
            string fun = "Write_To_PLC";

            if (PLC_Socket.Connect)
            {
                if (!PLC_Out.Write(PLC_Socket))
                    Log_Add(fun, "[PLC] Write_To_PLC Error.", emLog_Type.Error);
            }
        }
        private void Write_Recipe(string thread_Name)
        {
            string fun = "Write_Recipe";

            Log_Add(fun, string.Format("[PLC] Thread Name={0:s} in.", thread_Name));
            if (PLC_Socket.Connect)
            {
                if (PLC_In.Can_Change_Recipe)
                {
                    if (PLC_Recipe.Write(PLC_Socket))
                    {
                        PLC_Out.Write_Recipe.OK = true;
                        Log_Add(fun, "[PLC] PLC Recipe更新完成");
                    }
                    else
                    {
                        PLC_Out.Write_Recipe.OK = false;
                        Log_Add(fun, "[PLC] PLC Recipe更新失敗", emLog_Type.Error);
                    }
                }
                else
                {
                    PLC_Out.Write_Recipe.OK = false;
                    Log_Add(fun, "[PLC] PLC Recipe無法更新,請確認PLC狀態.", emLog_Type.Warning);
                }
            }
            else
            {
                PLC_Out.Write_Recipe.OK = false;
                Log_Add(fun, "[PLC] PLC  未連線, PLC Recipe更新失敗", emLog_Type.Warning);
            }
            PLC_Out.Write_Recipe.Finish = true;

        }

    }

    //-----------------------------------------------------------------------------------------------------
    // TPLC_Data_In
    //-----------------------------------------------------------------------------------------------------
    public class TPLC_Data_In : TPLC_Base_Data
    {
        public bool Write_Recipe_Req = false;
        public bool Can_Change_Recipe = false;

        public bool Printer_Req;
        public bool P_Read_Code_Req;
        public bool C_Read_Code_Req;

        public TPLC_Data_In_CIM_Data CIM_Data = new TPLC_Data_In_CIM_Data();

        public double LD_UD_IN_Z;
        public double LD_UD_OUT_Z;
        public double Get_Hand_X;
        public double Get_Hand_In_Y;
        public double Get_Hand_In_Z;
        //public double Out_Hand_X;
        public double Get_Hand_Out_Y;
        public double Get_Hand_Out_Z;
        public double Table_Y;
        public double Table_hold_X;//載台靠位
        public double Table_hold_Y;//載台靠位
        public double ULD_Y;
        public double ULD_Z;
        public double Zebra_Hand_X;//貼標手臂
        public double Zebra_Hand_Z;//貼標手臂
        public double Zebra_Hand_Q;//貼標手臂
        public TPLC_Data_In()
        {
        }
        public bool Read(TMelsec_QPLC_Eth_Connect plc)
        {
            bool result = false;
            ushort[] read_data = new ushort[Count];
            if (plc.Connect)
            {
                int c = Max_Count;
                result = plc.Read_Byte(Start_Code, ref read_data, Count);
                Array.Copy(read_data, 0, Data, 0, Count);
            }
            Update();
            return result;
            //bool result = false;

            //if (plc.Connect)
            //{
            //    result = plc.Read_Byte(Start_Code, ref Data, Count);
            //}
            //Update();
            //return result;
        }
        override public void Update()
        {
            Can_Change_Recipe       = PLC_Data_Tool.Get_Bit(Data, 0, 1);

            Printer_Req           = PLC_Data_Tool.Get_Bit(Data, 1, 8);
            P_Read_Code_Req       = PLC_Data_Tool.Get_Bit(Data, 2, 8);
            C_Read_Code_Req       = PLC_Data_Tool.Get_Bit(Data, 2, 9);
            LD_UD_IN_Z = PLC_Data_Tool.Get_DWord(Data, 200, 3);
            LD_UD_OUT_Z = PLC_Data_Tool.Get_DWord(Data, 202, 3);
            Get_Hand_X = PLC_Data_Tool.Get_DWord(Data, 204, 3);

            Get_Hand_In_Y = PLC_Data_Tool.Get_DWord(Data, 206, 3);
            Get_Hand_In_Z = PLC_Data_Tool.Get_DWord(Data, 208, 3);
            Get_Hand_Out_Y = PLC_Data_Tool.Get_DWord(Data, 210, 3);
            Get_Hand_Out_Z = PLC_Data_Tool.Get_DWord(Data, 212, 3);
            Table_Y = PLC_Data_Tool.Get_DWord(Data, 214, 3);
            Table_hold_X = PLC_Data_Tool.Get_DWord(Data, 216, 3);

            Table_hold_Y = PLC_Data_Tool.Get_DWord(Data, 218, 3);
            ULD_Y = PLC_Data_Tool.Get_DWord(Data, 220, 3);
            ULD_Z = PLC_Data_Tool.Get_DWord(Data, 222, 3);
            Zebra_Hand_X = PLC_Data_Tool.Get_DWord(Data, 224, 3);
            Zebra_Hand_Z = PLC_Data_Tool.Get_DWord(Data, 226, 3);
            Zebra_Hand_Q = PLC_Data_Tool.Get_DWord(Data, 228, 3);
            #region CIM_Data
            CIM_Data.Code = PLC_Data_Tool.Get_String(Data, 50, 20).Trim();//取得當前CIM資訊(Barcode)
            CIM_Data.Year = PLC_Data_Tool.Get_DWord(Data, 70);
            CIM_Data.Date = PLC_Data_Tool.Get_DWord(Data, 72);
            CIM_Data.Lot_ID = PLC_Data_Tool.Get_String(Data, 74, 10);
            CIM_Data.Work_ID = PLC_Data_Tool.Get_String(Data, 84, 10);
            CIM_Data.Ver = PLC_Data_Tool.Get_DWord(Data, 94);

            TPub.Recipe.Printer.Value_List.Set_Value("%LOT_ID%", CIM_Data.Lot_ID);
            TPub.Recipe.Printer.Value_List.Set_Value("%CODE_CIM%", CIM_Data.Code);//
            TPub.Recipe.Printer.Value_List.Set_Value("%Work_ID%", CIM_Data.Work_ID);//
            TPub.Recipe.Printer.Value_List.Set_Value("%VER%", CIM_Data.Ver.ToString());
            #endregion
        }
        public void View_Data(string filename)
        {
            TForm_Data_View form = new TForm_Data_View();
            form.Set_Param(this, TPub.Environment.Base.Database_Path + "In.csv");
            form.ShowDialog();
            //TForm_Data_View form = new TForm_Data_View();
            //form.Set_Param(this, filename);
            //form.ShowDialog();
        }
        public class TPLC_Data_In_CIM_Data
        {
            public string Code = "8888";     //Code
            public int Year = 0;         //年份
            public int Date = 0;         //日期
            public string Lot_ID = "9999";   //Lot_ID
            public string Work_ID = "7777";  //WORK_ID
            public int Ver = 0;          //版本

            public string Year_Str
            {
                get
                {
                    return Year.ToString();
                }
            }
            public string Date_Str
            {
                get
                {
                    return Date.ToString();
                }
            }
            public string Ver_Str
            {
                get
                {
                    return Ver.ToString();
                }
            }
            public TPLC_Data_In_CIM_Data()
            {

            }
        }

    }

    //-----------------------------------------------------------------------------------------------------
    // TPLC_Data_Out
    //-----------------------------------------------------------------------------------------------------
    public class TPLC_Data_Out : TPLC_Base_Data
    {
        #region Bit
        public bool On_Line;
        public TPLC_CMD_Data Write_Recipe = new TPLC_CMD_Data();
        public TPLC_CMD_Data Printer_Label = new TPLC_CMD_Data();
        public TPLC_CMD_Data P_Reader_Read = new TPLC_CMD_Data();
        public TPLC_CMD_Data C_Reader_Read = new TPLC_CMD_Data();

        public bool Printer_Ready = false;
        public bool Printer_Paper_Out = false;
        public bool Printer_Pause = false;
        public bool P_Reader_Ready = false;
        public bool C_Reader_Ready = false;
        #endregion

        #region Word
        public string P_Code = "";
        public string C_Code = "";
        #endregion

        public TPLC_Data_Out()
        {
        }
        public bool Write(TMelsec_QPLC_Eth_Connect plc)
        {
            bool result = false;

            Update();
            if (plc.Connect)
            {
                result = plc.Write_Byte(Start_Code, Data, Count);
            }
            return result;
        }
        override public void Update()
        {
            //Printer_Paper_Out = TPub.Printer.Status.Paper_Out;
            //Printer_Pause = TPub.Printer.Status.Pause;
            //P_Reader_Ready = TPub.Panel_Reader.Connect;
            Printer_Ready = TPub.Printer.Status.Ready;
            Printer_Pause = TPub.Printer.Status.Pause;
            Printer_Paper_Out = TPub.Printer.Status.Paper_Out;
        
            #region Bit
            #region Main
            PLC_Data_Tool.Set_Bit(Data, 0, 00, On_Line);
            PLC_Data_Tool.Set_Bit(Data, 0, 01, Write_Recipe.Finish);
       //    PLC_Data_Tool.Set_Bit(Data, 0, 02, Write_Recipe.OK);
            #endregion

            #region P_Reader
            PLC_Data_Tool.Set_Bit(Data, 2, 00, P_Reader_Ready);
            PLC_Data_Tool.Set_Bit(Data, 2, 8, P_Reader_Read.Finish);
           
            PLC_Data_Tool.Set_Bit(Data, 2, 9, P_Reader_Read.OK);
            #endregion

            #region C_Reader
            //PLC_Data_Tool.Set_Bit(Data, 4, 00, C_Reader_Read.Finish);
            //PLC_Data_Tool.Set_Bit(Data, 4, 15, C_Reader_Ready);

            //PLC_Data_Tool.Set_Bit(Data, 5, 00, C_Reader_Read.OK);
            #endregion

            #region Printer
            PLC_Data_Tool.Set_Bit(Data, 1, 00, Printer_Ready);
            PLC_Data_Tool.Set_Bit(Data, 1, 01, Printer_Paper_Out);
            PLC_Data_Tool.Set_Bit(Data, 1, 02, Printer_Pause);
            PLC_Data_Tool.Set_Bit(Data, 1, 8, Printer_Label.Finish);


            
            
            PLC_Data_Tool.Set_Bit(Data, 1, 9, Printer_Label.OK);
            #endregion
            #endregion

            #region Word
            PLC_Data_Tool.Set_String(Data, 10, 10, P_Code);
            PLC_Data_Tool.Set_String(Data, 20, 10, C_Code);
            #endregion
        }
        public void View_Data(string filename)
        {
            TForm_Data_View form = new TForm_Data_View();
            form.Set_Param(this, filename);
            form.ShowDialog();
        }
    }
    
    public class TPLC_CMD_Data
    {
        public bool Running;
        public bool Finish;
        public bool OK;
    }

    //-----------------------------------------------------------------------------------------------------
    // TPLC_Data_Recipe
    //-----------------------------------------------------------------------------------------------------
    public class TPLC_Data_Recipe : TPLC_Base_Data
    {
        public double Tray_Hand_Get_Full_X,
                      Tray_Hand_Get_Full_Y,
                      Tray_Hand_Get_Full_Z;
        public double Tray_Hand_Put_Full_Up_X,
                      Tray_Hand_Put_Full_Up_Y,
                      Tray_Hand_Put_Full_Up_Z;
        public double Tray_Hand_Put_Full_Dn_X,
                      Tray_Hand_Put_Full_Dn_Y,
                      Tray_Hand_Put_Full_Dn_Z;
        public double Tray_Hand_Put_Empty_X,
                      Tray_Hand_Put_Empty_Y,
                      Tray_Hand_Put_Empty_Z;
        
        public double Tray_Slider_Up_In,
                      Tray_Slider_Up_Out,
                      Tray_Slider_Dn_In,
                      Tray_Slider_Dn_Out;
        
        public double LightBar_Hand_Get_X,
                      LightBar_Hand_Get_Y,
                      LightBar_Hand_Get_Z, 
                      LightBar_Hand_Get_Q;
        public double LightBar_Hand_Put_X,
                      LightBar_Hand_Put_Y,
                      LightBar_Hand_Put_Z,
                      LightBar_Hand_Put_Q;
        
        public double Out_Hand_Get_X,
                      Out_Hand_Get_Y,
                      Out_Hand_Get_Z,
                      Out_Hand_Get_Q;        
        public double Out_Hand_Grab_X,
                      Out_Hand_Grab_Y,
                      Out_Hand_Grab_Z,
                      Out_Hand_Grab_Q;
        public double Out_Hand_Put_X,
                      Out_Hand_Put_Y,
                      Out_Hand_Put_Z,
                      Out_Hand_Put_Q;

        public TPLC_MS_Param MS_Param = new TPLC_MS_Param();

        public double CCD_X;

        public int LightBar_Count;
        public TPLC_Tray_Pos[] LightBar_Pos = new TPLC_Tray_Pos[30];

        public TPLC_Data_Recipe() 
        {
            for (int i = 0; i < LightBar_Pos.Length; i++) LightBar_Pos[i] = new TPLC_Tray_Pos();
        }
        public bool Write(TMelsec_QPLC_Eth_Connect plc)
        {
            bool result = false;

            Update();
            if (plc.Connect)
            {
                result = plc.Write_Byte(Start_Code, Data, Count);
            }
            return result;
        }
        override public void Update()
        {
            int no = 0;
            int start_no = 0;
            int tmp_no = 0;



            #region MS_Param
            start_no =0;
            for (int i = 0; i < MS_Param.Axis.Length; i++)
            {
                for (int j = 0; j < MS_Param.Axis[i].Pos.Length; j++)
                {
                    tmp_no = start_no + i * 20 + j * 2;
                    PLC_Data_Tool.Set_DWord(Data, tmp_no, 3, MS_Param.Axis[i].Pos[j]);
                }
            }
            #endregion
        }
        public void View_Data(string filename)
        {
            TForm_Data_View form = new TForm_Data_View();
            form.Set_Param(this, filename);
            form.ShowDialog();
        }

    }
    public class TPLC_Tray_Pos
    {
        public double X, Y, Q;
    }

    //-----------------------------------------------------------------------------------------------------
    // TPLC_Data_MS_Param
    //-----------------------------------------------------------------------------------------------------
    public class TPLC_MS_Param
    {
        public TPLC_MS_Param_Axis_Pos[] Axis = new TPLC_MS_Param_Axis_Pos[20];

        public TPLC_MS_Param()
        {
            for (int i = 0; i < Axis.Length; i++) Axis[i] = new TPLC_MS_Param_Axis_Pos();
        }
    }

    public class TPLC_MS_Param_Axis_Pos
    {
        public int Dot_Num = 3;
        public double[] Pos = new double[10];

        public TPLC_MS_Param_Axis_Pos()
        {

        }
    }
}