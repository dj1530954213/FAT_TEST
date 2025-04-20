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
                            await MockATestToTarget("111","01",pc,0f,4f);
                            await MockATestToTarget("113", "03", pc,0f,2f);
                            await MockATestToTarget("115", "05", pc,-40f,80f);
                            await MockATestToTarget("117", "07", pc,-40f,80f);
                            await MockATestToTarget("119", "09", pc,0f,100f);
                            await MockATestToTarget("121", "11", pc, 0f, 100f);

                            //AO点位映射
                            await MockATargetToTest("33", "101", pc, 0f, 100f);
                            await MockATargetToTest("35", "103", pc, 0f, 100f);
                            await MockATargetToTest("37", "105", pc, 0f, 100f);
                            await MockATargetToTest("39", "107", pc, 0f, 100f);

                            //DI点位映射
                            await MockDTestToTarget("131", "1");
                            await MockDTestToTarget("132", "2");
                            await MockDTestToTarget("133", "3");
                            await MockDTestToTarget("134", "4");
                            await MockDTestToTarget("135", "5");
                            await MockDTestToTarget("136", "6");
                            await MockDTestToTarget("137", "7");
                            await MockDTestToTarget("138", "8");
                            await MockDTestToTarget("139", "9");
                            await MockDTestToTarget("140", "10");
                            await MockDTestToTarget("141", "11");
                            await MockDTestToTarget("142", "12");
                            await MockDTestToTarget("143", "13");
                            await MockDTestToTarget("144", "14");
                            await MockDTestToTarget("145", "15");
                            await MockDTestToTarget("146", "16");
                            await MockDTestToTarget("147", "17");
                            await MockDTestToTarget("148", "18");
                            await MockDTestToTarget("149", "19");
                            await MockDTestToTarget("150", "20");

                            //DO点位映射
                            await MockDTargetToTest("33", "101");
                            await MockDTargetToTest("34", "102");
                            await MockDTargetToTest("35", "103");
                            await MockDTargetToTest("36", "104");
                            await MockDTargetToTest("37", "105");
                            await MockDTargetToTest("38", "106");
                            await MockDTargetToTest("39", "107");
                            await MockDTargetToTest("40", "108");
                            await MockDTargetToTest("41", "109");
                            await MockDTargetToTest("42", "110");
                            await MockDTargetToTest("43", "111");
                            await MockDTargetToTest("44", "112");
                            await MockDTargetToTest("45", "113");
                            await MockDTargetToTest("46", "114");
                            await MockDTargetToTest("47", "115");
                            await MockDTargetToTest("48", "116");
                            await MockDTargetToTest("49", "117");
                            await MockDTargetToTest("50", "118");
                            await MockDTargetToTest("51", "119");
                            await MockDTargetToTest("52", "120");
                            await MockDTargetToTest("53", "121");
                            await MockDTargetToTest("54", "122");
                            await MockDTargetToTest("55", "123");
                            await MockDTargetToTest("56", "124");
                            await MockDTargetToTest("57", "125");
                            await MockDTargetToTest("58", "126");
                            await MockDTargetToTest("59", "127");
                            await MockDTargetToTest("60", "128");


                            break;
                        case "2":
                            //MessageBox.Show("开始测试2批次");
                            //AI点位映射
                            await MockATestToTarget("111", "13", pc, 0f, 100f);
                            await MockATestToTarget("113", "15", pc, 0f, 100f);
                            await MockATestToTarget("115", "17", pc, 0f, 100f);
                            await MockATestToTarget("117", "19", pc, 0f, 10000f);
                            await MockATestToTarget("119", "21", pc, 0f, 20000f);
                            await MockATestToTarget("121", "23", pc, 0f, 100f);

                            //AO点位映射
                            await MockATargetToTest("41", "101", pc, 0f, 100f);
                            await MockATargetToTest("43", "103", pc, 0f, 100f);
                            await MockATargetToTest("45", "105", pc, 0f, 100f);
                            await MockATargetToTest("47", "107", pc, 0f, 100f);

                            //DI点位映射
                            await MockDTestToTarget("131", "21");
                            await MockDTestToTarget("132", "22");
                            await MockDTestToTarget("133", "23");
                            await MockDTestToTarget("134", "24");
                            await MockDTestToTarget("135", "25");
                            await MockDTestToTarget("136", "26");
                            await MockDTestToTarget("137", "27");
                            await MockDTestToTarget("138", "28");
                            await MockDTestToTarget("139", "29");
                            await MockDTestToTarget("140", "30");
                            await MockDTestToTarget("141", "31");
                            await MockDTestToTarget("142", "32");

                            //DO点位映射
                            await MockDTargetToTest("61", "101");
                            await MockDTargetToTest("62", "102");
                            await MockDTargetToTest("63", "103");
                            await MockDTargetToTest("64", "104");
                            break;
                        case "3":
                            //AI点位映射
                            await MockATestToTarget("111", "25", pc, 0f, 100f);
                            await MockATestToTarget("113", "27", pc, 0f, 100f);
                            await MockATestToTarget("115", "29", pc, 0f, 100f);
                            await MockATestToTarget("117", "31", pc, 0f, 100f);
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
