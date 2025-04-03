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
                    //float value1 = await modbusTcpCommunication.ReadAnalogValueAsync("17");
                    //await modbusTcpCommunication.WriteAnalogValueAsync("101", value1 * (1 - pc));

                    //float value2 = await modbusTcpCommunication.ReadAnalogValueAsync("103");
                    //await modbusTcpCommunication.WriteAnalogValueAsync("1", value2 * (1 - pc));

                    //if (!sd)
                    //{
                    //    bool value3 = await modbusTcpCommunication.ReadDigitalValueAsync("17");
                    //    await modbusTcpCommunication.WriteDigitalValueAsync("101", value3);

                    //    bool value4 = await modbusTcpCommunication.ReadDigitalValueAsync("102");
                    //    await modbusTcpCommunication.WriteDigitalValueAsync("1", value4);
                    //}
                    //await MockA();
                    switch (textBox_batch.Text)
                    {
                        case "1":
                            //AI点位映射
                            await MockA("111","01",pc);
                            await MockA("113", "03", pc);
                            await MockA("115", "05", pc);
                            await MockA("117", "07", pc);
                            await MockA("119", "09", pc);
                            await MockA("121", "11", pc);

                            //AO点位映射
                            await MockA("33", "101", pc);
                            await MockA("35", "103", pc);
                            await MockA("37", "105", pc);
                            await MockA("39", "107", pc);

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
                            //AI点位映射
                            await MockA("111", "13", pc);
                            await MockA("113", "15", pc);
                            await MockA("115", "17", pc);
                            await MockA("117", "19", pc);
                            await MockA("119", "21", pc);
                            await MockA("121", "23", pc);

                            //AO点位映射
                            await MockA("41", "101", pc);
                            await MockA("43", "103", pc);
                            await MockA("45", "105", pc);
                            await MockA("47", "107", pc);

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
                            await MockD("140", "20");
                            await MockD("141", "21");
                            await MockD("142", "22");

                            //DO点位映射
                            await MockD("61", "101");
                            await MockD("62", "102");
                            await MockD("63", "103");
                            await MockD("64", "104");
                            break;
                        case "3":
                            //AI点位映射
                            await MockA("111", "25", pc);
                            await MockA("113", "27", pc);
                            await MockA("115", "29", pc);
                            await MockA("117", "31", pc);
                            break;
                    }
                    await Task.Delay(100);
                }
            });
        }

        public async Task MockA(string readAddress,string writeAddress,float pc)
        {
            float value1 = await modbusTcpCommunication.ReadAnalogValueAsync(readAddress);
            await modbusTcpCommunication.WriteAnalogValueAsync(writeAddress, value1 * (1 - pc));
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
