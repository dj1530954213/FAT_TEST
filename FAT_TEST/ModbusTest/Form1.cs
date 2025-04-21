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
                            await MockDTestToTarget("401", "1");
                            await MockDTestToTarget("402", "2");
                            await MockDTestToTarget("403", "3");
                            await MockDTestToTarget("404", "4");
                            await MockDTestToTarget("405", "5");
                            await MockDTestToTarget("406", "6");
                            await MockDTestToTarget("407", "7");
                            await MockDTestToTarget("408", "8");
                            await MockDTestToTarget("409", "9");
                            await MockDTestToTarget("410", "10");
                            await MockDTestToTarget("411", "11");
                            await MockDTestToTarget("412", "12");
                            await MockDTestToTarget("413", "13");
                            await MockDTestToTarget("414", "14");
                            await MockDTestToTarget("415", "15");
                            await MockDTestToTarget("416", "16");

                            await MockDTestToTarget("301", "32");

                            

                            //DO点位映射
                            await MockDTargetToTest("33", "101");
                            await MockDTargetToTest("34", "102");
                            await MockDTargetToTest("35", "201");
                            await MockDTargetToTest("36", "202");
                            await MockDTargetToTest("37", "203");
                            await MockDTargetToTest("38", "204");
                            await MockDTargetToTest("39", "205");
                            await MockDTargetToTest("40", "206");
                            await MockDTargetToTest("41", "207");
                            await MockDTargetToTest("42", "208");
                            await MockDTargetToTest("43", "209");
                            await MockDTargetToTest("44", "210");
                            await MockDTargetToTest("45", "211");
                            await MockDTargetToTest("46", "212");
                            await MockDTargetToTest("47", "213");
                            await MockDTargetToTest("48", "214");
                            await MockDTargetToTest("49", "103");
                            await MockDTargetToTest("50", "104");
                            await MockDTargetToTest("51", "215");
                            await MockDTargetToTest("52", "216");




                            break;
                        case "2":
                            //MessageBox.Show("开始测试2批次");
                            //AI点位映射
                            await MockATestToTarget("301", "17", pc, 0f, 100f);
                            await MockATestToTarget("302", "19", pc, 0f, 10000f);

                            //AO点位映射


                            //DI点位映射
                            await MockDTestToTarget("401", "17");
                            await MockDTestToTarget("402", "18");
                            await MockDTestToTarget("403", "19");
                            await MockDTestToTarget("404", "20");
                            await MockDTestToTarget("405", "21");
                            await MockDTestToTarget("406", "22");
                            await MockDTestToTarget("407", "23");
                            await MockDTestToTarget("408", "24");
                            await MockDTestToTarget("409", "25");
                            await MockDTestToTarget("410", "26");
                            await MockDTestToTarget("411", "27");
                            await MockDTestToTarget("412", "28");
                            await MockDTestToTarget("413", "29");
                            await MockDTestToTarget("414", "30");
                            await MockDTestToTarget("415", "31");


                            //DO点位映射
                            await MockDTargetToTest("53", "201");
                            await MockDTargetToTest("54", "202");
                            await MockDTargetToTest("55", "203");
                            await MockDTargetToTest("56", "204");
                            await MockDTargetToTest("57", "205");
                            await MockDTargetToTest("58", "206");
                            await MockDTargetToTest("59", "207");
                            await MockDTargetToTest("60", "208");
                            await MockDTargetToTest("61", "209");
                            await MockDTargetToTest("62", "210");
                            await MockDTargetToTest("63", "211");
                            await MockDTargetToTest("64", "212");

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

        public async Task MockDTestToTarget(string readAddress, string writeAddress)
        {
            bool value3 = await modbusTcpCommunication.ReadDigitalValueAsync(readAddress);
            await modbusTcpCommunicationTagret.WriteDigitalValueAsync(writeAddress, value3);
        }

        public async Task MockDTargetToTest(string readAddress, string writeAddress)
        {
            bool value3 = await modbusTcpCommunicationTagret.ReadDigitalValueAsync(readAddress);
            await modbusTcpCommunication.WriteDigitalValueAsync(writeAddress, value3);
        }

        private void button_stop_Click(object sender, EventArgs e)
        {
            MessageBox.Show("停止执行");
            run = false;
        }
    }
}
