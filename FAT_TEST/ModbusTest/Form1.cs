namespace ModbusTest
{
    public partial class Form1 : Form
    {
        private static bool run = false;
        private ModbusTcpCommunication modbusTcpCommunication;
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            modbusTcpCommunication = new ModbusTcpCommunication();
        }

        private async void button_connect_Click(object sender, EventArgs e)
        {
            var result = await modbusTcpCommunication.ConnectAsync();
            if (result)
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
                    richTextBox_result.Text = $"批次{textBox_batch.Text}";
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
                            await MockD("131", "1");
                            await MockD("132", "2");
                            await MockD("133", "3");
                            await MockD("134", "4");
                            await MockD("135", "5");
                            await MockD("136", "6");
                            await MockD("137", "7");
                            await MockD("138", "8");
                            await MockD("139", "9");
                            await MockD("140", "10");
                            await MockD("141", "11");
                            await MockD("142", "12");
                            await MockD("143", "13");
                            await MockD("144", "14");
                            await MockD("145", "15");
                            await MockD("146", "16");
                            await MockD("147", "17");
                            await MockD("148", "18");
                            await MockD("149", "19");
                            await MockD("150", "20");

                            //DO点位映射
                            await MockD("33", "101");
                            await MockD("34", "102");
                            await MockD("35", "103");
                            await MockD("36", "104");
                            await MockD("37", "105");
                            await MockD("38", "106");
                            await MockD("39", "107");
                            await MockD("40", "108");
                            await MockD("41", "109");
                            await MockD("42", "110");
                            await MockD("43", "111");
                            await MockD("44", "112");
                            await MockD("45", "113");
                            await MockD("46", "114");
                            await MockD("47", "115");
                            await MockD("48", "116");
                            await MockD("49", "117");
                            await MockD("50", "118");
                            await MockD("51", "119");
                            await MockD("52", "120");
                            await MockD("53", "121");
                            await MockD("54", "122");
                            await MockD("55", "123");
                            await MockD("56", "124");
                            await MockD("57", "125");
                            await MockD("58", "126");
                            await MockD("59", "127");
                            await MockD("60", "128");


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
                            await MockD("131", "21");
                            await MockD("132", "22");
                            await MockD("133", "23");
                            await MockD("134", "24");
                            await MockD("135", "25");
                            await MockD("136", "26");
                            await MockD("137", "27");
                            await MockD("138", "28");
                            await MockD("139", "29");
                            await MockD("140", "30");
                            await MockD("141", "31");
                            await MockD("142", "32");

                            //DO点位映射
                            await MockD("61", "101");
                            await MockD("62", "102");
                            await MockD("63", "103");
                            await MockD("64", "104");
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
            await modbusTcpCommunication.WriteAnalogValueAsync(writeAddress, result);
        }
        //AO
        public async Task MockATargetToTest(string readAddress, string writeAddress, float pc, float min, float max)
        {
            float value1 = await modbusTcpCommunication.ReadAnalogValueAsync(readAddress);
            float result = (value1 - min) / (max - min) * 100;
            await modbusTcpCommunication.WriteAnalogValueAsync(writeAddress, result);
        }

        public async Task MockD(string readAddress, string writeAddress)
        {
            bool value3 = await modbusTcpCommunication.ReadDigitalValueAsync(readAddress);
            await modbusTcpCommunication.WriteDigitalValueAsync(writeAddress, value3);
        }

        private void button_stop_Click(object sender, EventArgs e)
        {
            MessageBox.Show("停止执行");
            run = false;
        }
    }
}
