namespace ModbusTest
{
    public partial class Form1 : Form
    {
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

        private async void button_write_Click(object sender, EventArgs e)
        {
            richTextBox_result.Text += (await modbusTcpCommunication.WriteAnalogValueAsync(textBox_address.Text, Convert.ToSingle(textBox_write.Text))).ToString();
            richTextBox_result.Text += "\n";
        }

        private async void button_read_Click(object sender, EventArgs e)
        {
            richTextBox_result.Text += (await modbusTcpCommunication.ReadAnalogValueAsync(textBox_read_address.Text)).ToString();
            richTextBox_result.Text += "\n";
        }
    }
}
