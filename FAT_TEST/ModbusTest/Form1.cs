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
                    float value1 = await modbusTcpCommunication.ReadAnalogValueAsync("17");
                    await modbusTcpCommunication.WriteAnalogValueAsync("101", value1 * (1 - pc));

                    float value2 = await modbusTcpCommunication.ReadAnalogValueAsync("103");
                    await modbusTcpCommunication.WriteAnalogValueAsync("1", value2 * (1 - pc));

                    if (!sd)
                    {
                        bool value3 = await modbusTcpCommunication.ReadDigitalValueAsync("17");
                        await modbusTcpCommunication.WriteDigitalValueAsync("101", value3);

                        bool value4 = await modbusTcpCommunication.ReadDigitalValueAsync("102");
                        await modbusTcpCommunication.WriteDigitalValueAsync("1", value4);
                    }
                    await Task.Delay(1000);
                }
            });
        }

        private void button_stop_Click(object sender, EventArgs e)
        {
            MessageBox.Show("停止执行");
            run = false;
        }
    }
}
