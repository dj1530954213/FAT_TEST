namespace ModbusTest
{
    public partial class Form1 : Form
    {
        private static bool run = false;
        private ModbusTcpCommunication modbusTcpCommunication;
        private ModbusTcpCommunication modbusTcpCommunicationTagret;
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            modbusTcpCommunication = new ModbusTcpCommunication();
            modbusTcpCommunicationTagret = new ModbusTcpCommunication();
        }

        private async void button_connect_Click(object sender, EventArgs e)
        {
            var result = await modbusTcpCommunication.ConnectAsync(true);
            var result2 = await modbusTcpCommunicationTagret.ConnectAsync(false);
            if (result && result2)
            {
                MessageBox.Show("连接成功");
            }
        }

        private async void button_disconnect_Click(object sender, EventArgs e)
        {
            var result = await modbusTcpCommunication.DisconnectAsync();
            if (result)
            {
                MessageBox.Show("断开连接成功");
            }
        }

        private async void button_start_Click(object sender, EventArgs e)
        {
            float pc = Convert.ToSingle(textBox_pc.Text);
            bool sd = Convert.ToBoolean(textBox_sd.Text);
            MessageBox.Show("开始执行");
            Task.Run(async () =>
            {
                run = true;
                while (run)
                {
                    //this.BeginInvoke(() =>
                    //{
                    //    richTextBox_result.Text += $"开始{textBox_batch.Text}批次映射";
                    //});
                    switch (textBox_batch.Text)
                    {
                        case "1":
                            //MessageBox.Show("开始测试1批次");
                            //AI点位映射
                            await MockATestToTarget("301","01",pc,0f,4f);
                            await MockATestToTarget("303", "03", pc,0f,2f);
                            await MockATestToTarget("305", "05", pc,-40f,80f);
                            await MockATestToTarget("307", "07", pc,-40f,80f);
                            await MockATestToTarget("309", "09", pc,0f,100f);
                            await MockATestToTarget("311", "11", pc, 0f, 100f);
                            await MockATestToTarget("313", "13", pc, 0f, 100f);
                            await MockATestToTarget("315", "15", pc, 0f, 100f);
                            await MockATestToTarget("201", "21", pc, 0f, 20000f);
                            await MockATestToTarget("203", "23", pc, 0f, 100f);
                            await MockATestToTarget("205", "25", pc, 0f, 100f);
                            await MockATestToTarget("207", "27", pc, 0f, 100f);
                            await MockATestToTarget("207", "29", pc, 0f, 100f);
                            await MockATestToTarget("209", "31", pc, 0f, 100f);

                            //AO点位映射
                            await MockATargetToTest("33", "101", pc, 0f, 100f);
                            await MockATargetToTest("35", "103", pc, 0f, 100f);
                            await MockATargetToTest("37", "105", pc, 0f, 100f);
                            await MockATargetToTest("39", "107", pc, 0f, 100f);
                            await MockATargetToTest("41", "109", pc, 0f, 100f);
                            await MockATargetToTest("43", "111", pc, 0f, 100f);
                            await MockATargetToTest("45", "113", pc, 0f, 100f);
                            await MockATargetToTest("47", "115", pc, 0f, 100f);

                            //DI点位映射
                            await MockDTestToTarget("401", "1",true);
                            await MockDTestToTarget("402", "2",false);
                            await MockDTestToTarget("403", "3",false);
                            await MockDTestToTarget("404", "4",false);
                            await MockDTestToTarget("405", "5",false);
                            await MockDTestToTarget("406", "6",false);
                            await MockDTestToTarget("407", "7",false);
                            await MockDTestToTarget("408", "8",false);
                            await MockDTestToTarget("409", "9", false);
                            await MockDTestToTarget("410", "10",false);
                            await MockDTestToTarget("411", "11",false);
                            await MockDTestToTarget("412", "12",false);
                            await MockDTestToTarget("413", "13",false);
                            await MockDTestToTarget("414", "14",false);
                            await MockDTestToTarget("415", "15",false);
                            await MockDTestToTarget("416", "16", false);

                            await MockDTestToTarget("301", "32", false);

                            

                            //DO点位映射
                            await MockDTargetToTest("33", "101",false);
                            await MockDTargetToTest("34", "102",false);
                            await MockDTargetToTest("35", "201",false);
                            await MockDTargetToTest("36", "202",false);
                            await MockDTargetToTest("37", "203",false);
                            await MockDTargetToTest("38", "204",false);
                            await MockDTargetToTest("39", "205",false);
                            await MockDTargetToTest("40", "206",false);
                            await MockDTargetToTest("41", "207",false);
                            await MockDTargetToTest("42", "208",false);
                            await MockDTargetToTest("43", "209",false);
                            await MockDTargetToTest("44", "210",false);
                            await MockDTargetToTest("45", "211",false);
                            await MockDTargetToTest("46", "212",false);
                            await MockDTargetToTest("47", "213",false);
                            await MockDTargetToTest("48", "214",false);
                            await MockDTargetToTest("49", "103",false);
                            await MockDTargetToTest("50", "104",false);
                            await MockDTargetToTest("51", "215",false);
                            await MockDTargetToTest("52", "216", false);




                            break;
                        case "2":
                            //MessageBox.Show("开始测试2批次");
                            //AI点位映射
                            await MockATestToTarget("301", "17", pc, 0f, 100f);
                            await MockATestToTarget("303", "19", pc, 0f, 10000f);

                            //AO点位映射


                            //DI点位映射
                            await MockDTestToTarget("401", "17",false);
                            await MockDTestToTarget("402", "18",false);
                            await MockDTestToTarget("403", "19",false);
                            await MockDTestToTarget("404", "20",false);
                            await MockDTestToTarget("405", "21",false);
                            await MockDTestToTarget("406", "22",false);
                            await MockDTestToTarget("407", "23",false);
                            await MockDTestToTarget("408", "24",false);
                            await MockDTestToTarget("409", "25",false);
                            await MockDTestToTarget("410", "26",false);
                            await MockDTestToTarget("411", "27",false);
                            await MockDTestToTarget("412", "28",false);
                            await MockDTestToTarget("413", "29",false);
                            await MockDTestToTarget("414", "30",false);
                            await MockDTestToTarget("415", "31", false);


                            //DO点位映射
                            await MockDTargetToTest("53", "201",false);
                            await MockDTargetToTest("54", "202",false);
                            await MockDTargetToTest("55", "203",false);
                            await MockDTargetToTest("56", "204",false);
                            await MockDTargetToTest("57", "205",false);
                            await MockDTargetToTest("58", "206",false);
                            await MockDTargetToTest("59", "207",false);
                            await MockDTargetToTest("60", "208",false);
                            await MockDTargetToTest("61", "209",false);
                            await MockDTargetToTest("62", "210",false);
                            await MockDTargetToTest("63", "211",false);
                            await MockDTargetToTest("64", "212", false);

                            break;
                        case "3":
                            //AI点位映射
                            
                            break;
                    }
                    await Task.Delay(2100);
                }
            });
        }
        //AI
        public async Task MockATestToTarget(string readAddress,string writeAddress,float pc,float min,float max)
        {
            float value1 = await modbusTcpCommunication.ReadAnalogValueAsync(readAddress);
            float result = min + (max - min) * value1 / 100f;
            await modbusTcpCommunicationTagret.WriteAnalogValueAsync(writeAddress, result);
        }
        //AO
        public async Task MockATargetToTest(string readAddress, string writeAddress, float pc, float min, float max)
        {
            float value1 = await modbusTcpCommunicationTagret.ReadAnalogValueAsync(readAddress);
            float result = (value1 - min) / (max - min) * 100;
            await modbusTcpCommunication.WriteAnalogValueAsync(writeAddress, result);
        }

        public async Task MockDTestToTarget(string readAddress, string writeAddress,bool diff)
        {
            bool value3 = await modbusTcpCommunication.ReadDigitalValueAsync(readAddress);
            await modbusTcpCommunicationTagret.WriteDigitalValueAsync(writeAddress, diff? !value3 : value3);
        }

        public async Task MockDTargetToTest(string readAddress, string writeAddress,bool diff)
        {
            bool value3 = await modbusTcpCommunicationTagret.ReadDigitalValueAsync(readAddress);
            await modbusTcpCommunication.WriteDigitalValueAsync(writeAddress, diff ? !value3 : value3);
        }

        private void button_stop_Click(object sender, EventArgs e)
        {
            MessageBox.Show("停止执行");
            run = false;
        }
    }
}
