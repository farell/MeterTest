using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace SattiliteDataAcquisition
{
    class MeterCom
    {
        private SerialPort comPort;
        private Form1 window;
        private System.Timers.Timer minuteTimer;
        private int period;
        private string portName;
        private byte[] buffer;

        private double workingConditionFlowAccum;
        private double standardConditionFlowAccum;
        private double workingConditionFlow;
        private double standardConditionFlow;
        private double temperature;
        private double pressure;
        private readonly int frameLength = 47;
        private int index;

        //
        public MeterCom(SerialPortConfig config, Form1 form1)
        {
            comPort = new SerialPort(config.portName, config.baudrate, config.parityCheck, config.databits, config.stopbits);
            comPort.DataReceived += new SerialDataReceivedEventHandler(ComDataReceive);
            comPort.ReadTimeout = 1500;
            this.buffer = new byte[1024];
            this.window = form1;
            this.portName = config.portName;
            this.period = config.period;
            this.index = 0;
            minuteTimer = new System.Timers.Timer(1000 * period);
            minuteTimer.Elapsed += new System.Timers.ElapsedEventHandler(MinuteTimer_TimesUp);
            minuteTimer.AutoReset = true; //每到指定时间Elapsed事件是触发一次（false），还是一直触发（true）
        }

        public void Start()
        {
            if (comPort != null)
            {
                if (!comPort.IsOpen)
                {
                    comPort.Open();
                }
            }
            if (!minuteTimer.Enabled)
            {
                minuteTimer.Start();
            }

        }

        public void Stop()
        {
            if (comPort != null)
            {
                if (comPort.IsOpen)
                {
                    comPort.Close();
                }
            }
            if (minuteTimer.Enabled)
            {
                minuteTimer.Stop();
            }
        }

        private void MinuteTimer_TimesUp(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (comPort.IsOpen)
            {
                byte[] cmd = { 0x17, 0x03, 0x00, 0x0A, 0x00, 0x15, 0xA6, 0xF1 };
                comPort.Write(cmd, 0, cmd.Length);
            }
        }

        private void ComDataReceive(object sender, SerialDataReceivedEventArgs e)
        {
            int workingConditionFlowAccumIndex = 17;
            int standardConditionFlowAccumIndex = 9;
            int workingConditionFlowIndex = 25;//4
            int standardConditionFlowIndex = 29;//0
            int temperatureIndex = 33;// 8;
            int pressureIndex = 37;// 12;
            int stateIndex = 41;
            int alarmIndex = 43;

            int bytesRead = 0;
            try
            {
                //this.comPort.Read()
                bytesRead = this.comPort.Read(this.buffer, index, 100);
                index = index + bytesRead;
            }
            catch(Exception ex)
            {
                window.AppendLog(ex.Message);
                index = 0;
                return;
            }

            if (index + 1 > frameLength - 1)
            {
                //int index = 25;
                byte[] byte1 = new byte[4];

                standardConditionFlowAccum = Process8Bytes(buffer, standardConditionFlowAccumIndex);
                workingConditionFlowAccum = Process8Bytes(buffer, workingConditionFlowAccumIndex);

                standardConditionFlow = Process4Bytes(buffer, standardConditionFlowIndex);

                workingConditionFlow = Process4Bytes(buffer, workingConditionFlowIndex);

                temperature = Process4Bytes(buffer, temperatureIndex);

                pressure = Process4Bytes(buffer, pressureIndex);
                index = 0;
                //}
                /*
                string str = System.Text.Encoding.ASCII.GetString(this.buffer,0,bytesRead);
                char[] chs = { ',' };
                char[] ch = { ':' };
                string[] splited = str.Split(chs);
                string stamp = splited[0];
                string voltage = splited[1].Split(ch)[1];
                string m0 = splited[4].Split(ch)[1];//time
                string m1 = splited[5].Split(ch)[1];
                string m2 = splited[6].Split(ch)[1];
                string m3 = splited[7].Split(ch)[1];
                string m4 = splited[8].Split(ch)[1];

                byte[] m0_byte = GetStringToBytes(m0);
                byte[] m1_byte = GetStringToBytes(m1);
                byte[] m2_byte = GetStringToBytes(m2);
                byte[] m3_byte = GetStringToBytes(m3);
                byte[] m4_byte = GetStringToBytes(m4);

                standardConditionFlowAccum = Process8Bytes(m0_byte, standardConditionFlowAccumIndex);
                workingConditionFlowAccum = Process8Bytes(m0_byte, workingConditionFlowAccumIndex);

                standardConditionFlow = Process4Bytes(m1_byte, standardConditionFlowIndex);
                workingConditionFlow = Process4Bytes(m1_byte, workingConditionFlowIndex);

                temperature = Process4Bytes(m1_byte, temperatureIndex);
                pressure = Process4Bytes(m1_byte, pressureIndex);

                int state_gas_volum_shortage = (m2_byte[1]&0x10) == 0x10 ? 1 : 0;
                int state_gas_over_use = (m2_byte[1] & 0x08) == 0x08 ? 1 : 0;
                int state_communication = (m2_byte[1] & 0x04) == 0x04 ? 1 : 0;
                int state_valve_open = (m2_byte[1] & 0x03);// == 0x03 ? 1 : 0;

                string valve_state = "未知";

                if(state_valve_open == 2)
                {
                    valve_state = "关闭";
                }else if(state_valve_open == 1)
                {
                    valve_state = "打开";
                }
                else if(state_valve_open == 3)
                {
                    valve_state = "异常";
                }
                else
                {
                    valve_state = "未知";
                }

                int alarm_external_power_lose = (m2_byte[2] & 0x04) == 0x04 ? 1 : 0;
                int alarm_calculate_battery_drain = (m2_byte[2] & 0x02) == 0x02 ? 1 : 0;
                int alarm_control_battery_drain = (m2_byte[2] & 0x01) == 0x01 ? 1 : 0;

                int alarm_tempreture_sensor_failed = (m2_byte[3] & 0x80) == 0x80 ? 1 : 0;
                int alarm_pressure_sensor_failed = (m2_byte[3] & 0x40) == 0x40 ? 1 : 0;
                int alarm_valve_failed = (m2_byte[3] & 0x20) == 0x20 ? 1 : 0;
                int alarm_control_low_power = (m2_byte[3] & 0x10) == 0x10 ? 1 : 0;
                int alarm_calculate_low_power = (m2_byte[3] & 0x08) == 0x08 ? 1 : 0;
                int alarm_pressure_too_high = (m2_byte[3] & 0x04) == 0x04 ? 1 : 0;
                int alarm_tempreture_too_high = (m2_byte[3] & 0x02) == 0x02 ? 1 : 0;
                int alarm_instant_working_condition_flow_exceed = (m2_byte[3] & 0x01) == 0x01 ? 1 : 0;

                */

                string result = DateTime.Now.ToString() + "  " + portName + " :\r\n";
                //result += "-电池电压 : " + int.Parse(voltage)/1000.0 + " V\r\n";
                result += "-工况总累积量 : " + workingConditionFlowAccum + "\r\n";
                result += "-工况瞬时流量 :" + workingConditionFlow + "\r\n";
                result += "-标况累积流量 :" + standardConditionFlowAccum + "\r\n";
                result += "-标况瞬时流量 :" + standardConditionFlow + "\r\n";
                result += "-燃气温度 :" + temperature + "\r\n";
                result += "-燃气绝对压力 :" + pressure + "\r\n";

                DateTime date = DateTime.Now.Date;
                //PlotData1("工况瞬时流量", workingConditionFlow, date);
                //PlotData2("标况瞬时流量", workingConditionFlow, date);
                //PlotData3("燃气温度", temperature, date);
                //PlotData4("燃气绝对压力", pressure, date);
                window.AppendPressure(Math.Round(pressure,2));
                window.AppendTemperature(Math.Round(temperature, 2));
                window.AppendWorkFlow(Math.Round(workingConditionFlow, 2));
                window.AppendWorkFlowAccum(Math.Round(workingConditionFlowAccum, 4));
                window.AppendStandardFlow(Math.Round(standardConditionFlow, 2));
                window.AppendStandardFlowAccum(Math.Round(standardConditionFlowAccum, 4));


                {
                    this.window.AppendLog(result);
                }
            }
            /*
            result += "-购气提示状态 : " + state_gas_volum_shortage + "\r\n";
            result += "-透支状态 :" + state_gas_over_use + "\r\n";
            result += "-通讯状态（卡控与积算仪之间）:" + state_communication + "\r\n";
            result += "-阀门状态 :" + valve_state + "\r\n";

            result += "-温度传感器故障 :" + alarm_tempreture_sensor_failed + "\r\n";
            result += "-压力传感器故障 :" + alarm_pressure_sensor_failed + "\r\n";
            result += "-阀门故障 : " + alarm_valve_failed + "\r\n";
            result += "-卡控电量不足 : " + alarm_control_low_power + "\r\n";
            result += "-积算仪电量不足 :" + alarm_calculate_low_power + "\r\n";
            result += "-压力超上限报警 :" + alarm_pressure_too_high + "\r\n";
            result += "-温度超上限报警 : " + alarm_tempreture_too_high + "\r\n";
            result += "-瞬时工况超流量上限报警 :" + alarm_instant_working_condition_flow_exceed + "\r\n";
            result += "-外电源失电 :" + alarm_external_power_lose + "\r\n";
            result += "-更换积算仪电池 : " + alarm_calculate_battery_drain + "\r\n";
            result += "-更换卡控电池 : " + alarm_control_battery_drain + "\r\n";
            */

            //foreach (var item in splited)
            
        }

        private void PlotData1(string name,double data,DateTime date)
        {
            if (data < 0.1)
            {
                return;
            }
            //window.AppendChartData1(name, Math.Round(data,3), date);
        }
        private void PlotData2(string name, double data, DateTime date)
        {
            if (data < 0.1)
            {
                return;
            }
            //window.AppendChartData2(name, Math.Round(data, 3), date);
        }
        private void PlotData3(string name, double data, DateTime date)
        {
            if (data < 0.1)
            {
                return;
            }
            //window.AppendChartData3(name, Math.Round(data, 3), date);
        }
        private void PlotData4(string name, double data, DateTime date)
        {
            if (data < 0.1)
            {
                return;
            }
            //window.AppendChartData4(name, Math.Round(data, 3), date);
        }


        private double Process4Bytes(byte[] buffer, int index)
        {
            byte[] temp = new byte[4];
            Buffer.BlockCopy(buffer, index, temp, 0, 4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(temp);
            }
            Single value = BitConverter.ToSingle(temp, 0);
            return (double)value;
        }

        private double Process8Bytes(byte[] buffer, int index)
        {
            byte[] temp = new byte[8];
            Buffer.BlockCopy(buffer, index, temp, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(temp);
            }
            double value = BitConverter.ToDouble(temp, 0);
            return value;
        }

        public static byte[] GetStringToBytes(string value)
        {
            SoapHexBinary shb = SoapHexBinary.Parse(value);
            return shb.Value;
        }

        public static string GetBytesToString(byte[] value)
        {
            SoapHexBinary shb = new SoapHexBinary(value);
            return shb.ToString();
        }
}

    class SerialPortConfig
    {
        public string portName;
        public int baudrate;
        public Parity parityCheck;
        public int databits;
        public StopBits stopbits;
        public int period;

        public SerialPortConfig(string port, string baudrate, string parity, string databits, string sb,string period)
        {
            this.portName = port;
            this.baudrate = Int32.Parse(baudrate);
            this.databits = Int32.Parse(databits);
            this.period = Int32.Parse(period);
            switch (parity)
            {
                case "None":
                    parityCheck = Parity.None;
                    break;
                case "Odd":
                    parityCheck = Parity.Odd;
                    break;
                case "Even":
                    parityCheck = Parity.Even;
                    break;
                case "Mark":
                    parityCheck = Parity.Mark;
                    break;
                case "Space":
                    parityCheck = Parity.Space;
                    break;
                default:
                    parityCheck = Parity.None;
                    break;
            }

            switch (sb)
            {
                case "1":
                    stopbits = StopBits.One;
                    break;
                case "1.5":
                    stopbits = StopBits.OnePointFive;
                    break;
                case "2":
                    stopbits = StopBits.Two;
                    break;
                default:
                    stopbits = StopBits.One;
                    break;
            }
        }
    }
}
