namespace ModbusTest
{
    public partial class Form1 : Form
    {
        private ModbusTcpCommunication modbusTcpCommunication;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            modbusTcpCommunication = = new ModbusTcpCommunication();
        }
    }
}
