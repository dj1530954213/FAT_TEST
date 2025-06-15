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
                    switch (textBox_batch.Text)
                    {
                        #region 原始映射关系
                        //case "1":
                        //    //MessageBox.Show("开始测试1批次");
                        //    //AI点位映射
                        //    await MockATestToTarget("301","01",pc,0f,4f);
                        //    await MockATestToTarget("303", "03", pc,0f,2f);
                        //    await MockATestToTarget("305", "05", pc,-40f,80f);
                        //    await MockATestToTarget("307", "07", pc,-40f,80f);
                        //    await MockATestToTarget("309", "09", pc,0f,100f);
                        //    await MockATestToTarget("311", "11", pc, 0f, 100f);
                        //    await MockATestToTarget("313", "13", pc, 0f, 100f);
                        //    await MockATestToTarget("315", "15", pc, 0f, 100f);
                        //    await MockATestToTarget("201", "21", pc, 0f, 20000f);
                        //    await MockATestToTarget("203", "23", pc, 0f, 100f);
                        //    await MockATestToTarget("205", "25", pc, 0f, 100f);
                        //    await MockATestToTarget("207", "27", pc, 0f, 100f);
                        //    await MockATestToTarget("207", "29", pc, 0f, 100f);
                        //    await MockATestToTarget("209", "31", pc, 0f, 100f);

                        //    //AO点位映射
                        //    await MockATargetToTest("33", "101", pc, 0f, 100f);
                        //    await MockATargetToTest("35", "103", pc, 0f, 100f);
                        //    await MockATargetToTest("37", "105", pc, 0f, 100f);
                        //    await MockATargetToTest("39", "107", pc, 0f, 100f);
                        //    await MockATargetToTest("41", "109", pc, 0f, 100f);
                        //    await MockATargetToTest("43", "111", pc, 0f, 100f);
                        //    await MockATargetToTest("45", "113", pc, 0f, 100f);
                        //    await MockATargetToTest("47", "115", pc, 0f, 100f);

                        //    //DI点位映射
                        //    await MockDTestToTarget("401", "1",true);
                        //    await MockDTestToTarget("402", "2",false);
                        //    await MockDTestToTarget("403", "3",false);
                        //    await MockDTestToTarget("404", "4",false);
                        //    await MockDTestToTarget("405", "5",false);
                        //    await MockDTestToTarget("406", "6",false);
                        //    await MockDTestToTarget("407", "7",false);
                        //    await MockDTestToTarget("408", "8",false);
                        //    await MockDTestToTarget("409", "9", false);
                        //    await MockDTestToTarget("410", "10",false);
                        //    await MockDTestToTarget("411", "11",false);
                        //    await MockDTestToTarget("412", "12",false);
                        //    await MockDTestToTarget("413", "13",false);
                        //    await MockDTestToTarget("414", "14",false);
                        //    await MockDTestToTarget("415", "15",false);
                        //    await MockDTestToTarget("416", "16", false);

                        //    await MockDTestToTarget("301", "32", false);



                        //    //DO点位映射
                        //    await MockDTargetToTest("33", "101",false);
                        //    await MockDTargetToTest("34", "102",false);
                        //    await MockDTargetToTest("35", "201",false);
                        //    await MockDTargetToTest("36", "202",false);
                        //    await MockDTargetToTest("37", "203",false);
                        //    await MockDTargetToTest("38", "204",false);
                        //    await MockDTargetToTest("39", "205",false);
                        //    await MockDTargetToTest("40", "206",false);
                        //    await MockDTargetToTest("41", "207",false);
                        //    await MockDTargetToTest("42", "208",false);
                        //    await MockDTargetToTest("43", "209",false);
                        //    await MockDTargetToTest("44", "210",false);
                        //    await MockDTargetToTest("45", "211",false);
                        //    await MockDTargetToTest("46", "212",false);
                        //    await MockDTargetToTest("47", "213",false);
                        //    await MockDTargetToTest("48", "214",false);
                        //    await MockDTargetToTest("49", "103",false);
                        //    await MockDTargetToTest("50", "104",false);
                        //    await MockDTargetToTest("51", "215",false);
                        //    await MockDTargetToTest("52", "216", false);




                        //    break;
                        //case "2":
                        //    //MessageBox.Show("开始测试2批次");
                        //    //AI点位映射
                        //    await MockATestToTarget("301", "17", pc, 0f, 100f);
                        //    await MockATestToTarget("303", "19", pc, 0f, 10000f);

                        //    //AO点位映射


                        //    //DI点位映射
                        //    await MockDTestToTarget("401", "17",false);
                        //    await MockDTestToTarget("402", "18",false);
                        //    await MockDTestToTarget("403", "19",false);
                        //    await MockDTestToTarget("404", "20",false);
                        //    await MockDTestToTarget("405", "21",false);
                        //    await MockDTestToTarget("406", "22",false);
                        //    await MockDTestToTarget("407", "23",false);
                        //    await MockDTestToTarget("408", "24",false);
                        //    await MockDTestToTarget("409", "25",false);
                        //    await MockDTestToTarget("410", "26",false);
                        //    await MockDTestToTarget("411", "27",false);
                        //    await MockDTestToTarget("412", "28",false);
                        //    await MockDTestToTarget("413", "29",false);
                        //    await MockDTestToTarget("414", "30",false);
                        //    await MockDTestToTarget("415", "31", false);


                        //    //DO点位映射
                        //    await MockDTargetToTest("53", "201",false);
                        //    await MockDTargetToTest("54", "202",false);
                        //    await MockDTargetToTest("55", "203",false);
                        //    await MockDTargetToTest("56", "204",false);
                        //    await MockDTargetToTest("57", "205",false);
                        //    await MockDTargetToTest("58", "206",false);
                        //    await MockDTargetToTest("59", "207",false);
                        //    await MockDTargetToTest("60", "208",false);
                        //    await MockDTargetToTest("61", "209",false);
                        //    await MockDTargetToTest("62", "210",false);
                        //    await MockDTargetToTest("63", "211",false);
                        //    await MockDTargetToTest("64", "212", false);

                        //    break;
                        //case "3":
                        //    //AI点位映射
                        //    break;

                        #endregion

                        case "1":
                            //MessageBox.Show("开始测试1批次");
                            //AI点位映射
                            await MockATestToTarget("0301", "0001", pc, 0.0f, 6.0f);
                            await MockATestToTarget("0303", "0003", pc, 0.0f, 6.0f);
                            await MockATestToTarget("0305", "0005", pc, -20.0f, 80.0f);
                            await MockATestToTarget("0307", "0007", pc, 0.0f, 0.6f);
                            await MockATestToTarget("0309", "0009", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0311", "0011", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0313", "0013", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0315", "0015", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0201", "0049", pc, -20.0f, 80.0f);
                            await MockATestToTarget("0203", "0051", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0205", "0053", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0207", "0055", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0209", "0057", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0211", "0059", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0213", "0061", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0215", "0063", pc, 0.0f, 100.0f);

                            //AO点位映射
                            await MockATargetToTest("0081", "0101", pc, 0.0f, 100.0f);
                            await MockATargetToTest("0083", "0103", pc, 0.0f, 100.0f);
                            await MockATargetToTest("0085", "0105", pc, 0.0f, 100.0f);
                            await MockATargetToTest("0087", "0107", pc, 0.0f, 100.0f);
                            await MockATargetToTest("0089", "0109", pc, 0.0f, 100.0f);
                            await MockATargetToTest("0091", "0111", pc, 0.0f, 100.0f);
                            await MockATargetToTest("0093", "0113", pc, 0.0f, 100.0f);
                            await MockATargetToTest("0095", "0115", pc, 0.0f, 100.0f);

                            //DI点位映射
                            await MockDTestToTarget("401", "1", false);
                            await MockDTestToTarget("402", "2", false);
                            await MockDTestToTarget("403", "3", false);
                            await MockDTestToTarget("405", "5", false);
                            await MockDTestToTarget("406", "6", false);
                            await MockDTestToTarget("407", "7", false);
                            await MockDTestToTarget("408", "8", false);
                            await MockDTestToTarget("409", "9", false);
                            await MockDTestToTarget("410", "10", false);
                            await MockDTestToTarget("411", "11", false);
                            await MockDTestToTarget("412", "12", false);
                            await MockDTestToTarget("413", "13", false);
                            await MockDTestToTarget("414", "14", false);
                            await MockDTestToTarget("415", "15", false);
                            await MockDTestToTarget("416", "16", false);

                            //DO点位映射
                            await MockDTargetToTest("17", "101", false);
                            await MockDTargetToTest("18", "102", false);
                            await MockDTargetToTest("19", "103", false);
                            await MockDTargetToTest("20", "104", false);
                            await MockDTargetToTest("21", "105", false);
                            await MockDTargetToTest("22", "106", false);
                            await MockDTargetToTest("23", "107", false);
                            await MockDTargetToTest("24", "108", false);
                            await MockDTargetToTest("25", "109", false);
                            await MockDTargetToTest("26", "110", false);
                            await MockDTargetToTest("27", "111", false);
                            await MockDTargetToTest("28", "112", false);
                            await MockDTargetToTest("29", "113", false);
                            await MockDTargetToTest("30", "114", false);
                            await MockDTargetToTest("31", "115", false);
                            await MockDTargetToTest("32", "116", false);
                            await MockDTargetToTest("9", "201", false);
                            await MockDTargetToTest("50", "202", false);
                            await MockDTargetToTest("51", "203", false);
                            await MockDTargetToTest("52", "204", false);
                            await MockDTargetToTest("53", "205", false);
                            await MockDTargetToTest("54", "206", false);
                            await MockDTargetToTest("55", "207", false);
                            await MockDTargetToTest("56", "208", false);
                            await MockDTargetToTest("57", "209", false);
                            await MockDTargetToTest("58", "210", false);
                            await MockDTargetToTest("59", "211", false);
                            await MockDTargetToTest("60", "212", false);
                            await MockDTargetToTest("61", "213", false);
                            await MockDTargetToTest("62", "214", false);
                            await MockDTargetToTest("63", "215", false);
                            await MockDTargetToTest("64", "216", false);
                            break;

                        case "2":
                            //MessageBox.Show("开始测试2批次");
                            //AI点位映射
                            await MockATestToTarget("0301", "0017", pc, -20.0f, 80.0f);
                            await MockATestToTarget("0303", "0019", pc, -20.0f, 80.0f);
                            await MockATestToTarget("0305", "0021", pc, 0.0f, 6.0f);
                            await MockATestToTarget("0307", "0023", pc, 0.0f, 6.0f);
                            await MockATestToTarget("0309", "0025", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0311", "0027", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0313", "0029", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0315", "0031", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0201", "0065", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0203", "0067", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0205", "0069", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0207", "0071", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0209", "0073", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0211", "0075", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0213", "0077", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0215", "0079", pc, 0.0f, 100.0f);

                            //DI点位映射
                            await MockDTestToTarget("401", "33", false);
                            await MockDTestToTarget("402", "34", false);
                            await MockDTestToTarget("403", "35", false);
                            await MockDTestToTarget("404", "36", false);
                            await MockDTestToTarget("405", "37", false);
                            await MockDTestToTarget("406", "38", false);
                            await MockDTestToTarget("407", "39", false);
                            await MockDTestToTarget("408", "0", false);
                            await MockDTestToTarget("409", "1", false);
                            await MockDTestToTarget("410", "2", false);
                            await MockDTestToTarget("411", "3", false);
                            await MockDTestToTarget("412", "4", false);
                            await MockDTestToTarget("413", "5", false);
                            await MockDTestToTarget("414", "6", false);
                            await MockDTestToTarget("415", "7", false);
                            await MockDTestToTarget("416", "8", false);

                            //DO点位映射
                            await MockDTargetToTest("161", "201", false);
                            await MockDTargetToTest("162", "202", false);
                            await MockDTargetToTest("163", "203", false);
                            await MockDTargetToTest("164", "204", false);
                            await MockDTargetToTest("165", "205", false);
                            await MockDTargetToTest("166", "206", false);
                            await MockDTargetToTest("167", "207", false);
                            await MockDTargetToTest("168", "208", false);
                            await MockDTargetToTest("169", "209", false);
                            await MockDTargetToTest("170", "210", false);
                            await MockDTargetToTest("171", "211", false);
                            await MockDTargetToTest("172", "212", false);
                            await MockDTargetToTest("173", "213", false);
                            await MockDTargetToTest("174", "214", false);
                            await MockDTargetToTest("175", "215", false);
                            await MockDTargetToTest("176", "216", false);
                            await MockDTargetToTest("177", "101", false);
                            await MockDTargetToTest("178", "102", false);
                            await MockDTargetToTest("179", "103", false);
                            await MockDTargetToTest("180", "104", false);
                            await MockDTargetToTest("181", "105", false);
                            await MockDTargetToTest("182", "106", false);
                            await MockDTargetToTest("183", "107", false);
                            await MockDTargetToTest("184", "108", false);
                            await MockDTargetToTest("185", "109", false);
                            await MockDTargetToTest("186", "110", false);
                            await MockDTargetToTest("187", "111", false);
                            await MockDTargetToTest("188", "112", false);
                            await MockDTargetToTest("189", "113", false);
                            await MockDTargetToTest("190", "114", false);
                            await MockDTargetToTest("191", "115", false);
                            await MockDTargetToTest("192", "116", false);
                            break;

                        case "3":
                            //MessageBox.Show("开始测试3批次");
                            //AI点位映射
                            await MockATestToTarget("0301", "0033", pc, 0.0f, 6.0f);
                            await MockATestToTarget("0303", "0035", pc, 0.0f, 6.0f);
                            await MockATestToTarget("0305", "0037", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0307", "0039", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0309", "0041", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0311", "0043", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0313", "0045", pc, 0.0f, 100.0f);
                            await MockATestToTarget("0315", "0047", pc, 0.0f, 100.0f);

                            //DI点位映射
                            await MockDTestToTarget("401", "65", true);
                            await MockDTestToTarget("402", "66", false);
                            await MockDTestToTarget("403", "67", false);
                            await MockDTestToTarget("404", "68", true);
                            await MockDTestToTarget("405", "69", false);
                            await MockDTestToTarget("406", "70", false);
                            await MockDTestToTarget("407", "71", true);
                            await MockDTestToTarget("408", "72", true);
                            await MockDTestToTarget("409", "73", false);
                            await MockDTestToTarget("410", "74", false);
                            await MockDTestToTarget("411", "75", false);
                            await MockDTestToTarget("412", "76", false);
                            await MockDTestToTarget("413", "77", false);
                            await MockDTestToTarget("414", "78", false);
                            await MockDTestToTarget("415", "79", false);
                            await MockDTestToTarget("416", "80", false);
                            break;

                        case "4":
                            //MessageBox.Show("开始测试4批次");
                            //DI点位映射
                            await MockDTestToTarget("401", "81", true);
                            await MockDTestToTarget("402", "82", false);
                            await MockDTestToTarget("403", "83", false);
                            await MockDTestToTarget("404", "84", true);
                            await MockDTestToTarget("405", "85", true);
                            await MockDTestToTarget("406", "86", true);
                            await MockDTestToTarget("407", "87", false);
                            await MockDTestToTarget("408", "88", false);
                            await MockDTestToTarget("409", "89", true);
                            await MockDTestToTarget("410", "90", true);
                            await MockDTestToTarget("411", "91", true);
                            await MockDTestToTarget("412", "92", true);
                            await MockDTestToTarget("413", "93", true);
                            await MockDTestToTarget("414", "94", true);
                            await MockDTestToTarget("415", "95", true);
                            await MockDTestToTarget("416", "96", true);
                            break;

                        case "5":
                            //MessageBox.Show("开始测试5批次");
                            //DI点位映射
                            await MockDTestToTarget("401", "97", true);
                            await MockDTestToTarget("402", "98", false);
                            await MockDTestToTarget("403", "99", false);
                            await MockDTestToTarget("404", "100", true);
                            await MockDTestToTarget("405", "101", true);
                            await MockDTestToTarget("406", "102", false);
                            await MockDTestToTarget("407", "103", true);
                            await MockDTestToTarget("408", "104", true);
                            await MockDTestToTarget("409", "105", false);
                            await MockDTestToTarget("410", "106", false);
                            await MockDTestToTarget("411", "107", true);
                            await MockDTestToTarget("412", "108", true);
                            await MockDTestToTarget("413", "109", true);
                            await MockDTestToTarget("414", "110", true);
                            await MockDTestToTarget("415", "111", true);
                            await MockDTestToTarget("416", "112", true);
                            break;

                        case "6":
                            //MessageBox.Show("开始测试6批次");
                            //DI点位映射
                            await MockDTestToTarget("401", "113", true);
                            await MockDTestToTarget("402", "114", true);
                            await MockDTestToTarget("403", "115", true);
                            await MockDTestToTarget("404", "116", true);
                            await MockDTestToTarget("405", "117", true);
                            await MockDTestToTarget("406", "118", true);
                            await MockDTestToTarget("407", "119", true);
                            await MockDTestToTarget("408", "120", true);
                            await MockDTestToTarget("409", "121", true);
                            await MockDTestToTarget("410", "122", true);
                            await MockDTestToTarget("411", "123", true);
                            await MockDTestToTarget("412", "124", true);
                            await MockDTestToTarget("413", "125", true);
                            await MockDTestToTarget("414", "126", true);
                            await MockDTestToTarget("415", "127", true);
                            await MockDTestToTarget("416", "128", true);
                            break;

                        case "7":
                            //MessageBox.Show("开始测试7批次");
                            //DI点位映射
                            await MockDTestToTarget("401", "129", true);
                            await MockDTestToTarget("402", "130", false);
                            await MockDTestToTarget("403", "131", false);
                            await MockDTestToTarget("404", "132", true);
                            await MockDTestToTarget("405", "133", false);
                            await MockDTestToTarget("406", "134", false);
                            await MockDTestToTarget("407", "135", true);
                            await MockDTestToTarget("408", "136", true);
                            await MockDTestToTarget("409", "137", true);
                            await MockDTestToTarget("410", "138", true);
                            await MockDTestToTarget("411", "139", true);
                            await MockDTestToTarget("412", "140", true);
                            await MockDTestToTarget("413", "141", true);
                            await MockDTestToTarget("414", "142", true);
                            await MockDTestToTarget("415", "143", true);
                            await MockDTestToTarget("416", "144", true);
                            break;

                        case "8":
                            //MessageBox.Show("开始测试8批次");
                            //DI点位映射
                            await MockDTestToTarget("401", "145", false);
                            await MockDTestToTarget("402", "146", true);
                            await MockDTestToTarget("403", "147", true);
                            await MockDTestToTarget("404", "148", false);
                            await MockDTestToTarget("405", "149", false);
                            await MockDTestToTarget("406", "150", false);
                            await MockDTestToTarget("407", "151", false);
                            await MockDTestToTarget("408", "152", false);
                            await MockDTestToTarget("409", "153", false);
                            await MockDTestToTarget("410", "154", false);
                            await MockDTestToTarget("411", "155", false);
                            await MockDTestToTarget("412", "156", false);
                            await MockDTestToTarget("413", "157", false);
                            await MockDTestToTarget("414", "158", false);
                            await MockDTestToTarget("415", "159", false);
                            await MockDTestToTarget("416", "160", false);
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
