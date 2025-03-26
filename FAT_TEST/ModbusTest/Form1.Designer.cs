namespace ModbusTest
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            button_connect = new Button();
            button_disconnect = new Button();
            richTextBox_result = new RichTextBox();
            textBox_write = new TextBox();
            label1 = new Label();
            label2 = new Label();
            textBox_address = new TextBox();
            button_write = new Button();
            label3 = new Label();
            textBox_read_address = new TextBox();
            button_read = new Button();
            SuspendLayout();
            // 
            // button_connect
            // 
            button_connect.Location = new Point(12, 12);
            button_connect.Name = "button_connect";
            button_connect.Size = new Size(284, 101);
            button_connect.TabIndex = 0;
            button_connect.Text = "连接ModbusTcp";
            button_connect.UseVisualStyleBackColor = true;
            button_connect.Click += button_connect_Click;
            // 
            // button_disconnect
            // 
            button_disconnect.Location = new Point(302, 12);
            button_disconnect.Name = "button_disconnect";
            button_disconnect.Size = new Size(284, 101);
            button_disconnect.TabIndex = 1;
            button_disconnect.Text = "断开ModbusTcp";
            button_disconnect.UseVisualStyleBackColor = true;
            button_disconnect.Click += button_disconnect_Click;
            // 
            // richTextBox_result
            // 
            richTextBox_result.Location = new Point(7, 440);
            richTextBox_result.Name = "richTextBox_result";
            richTextBox_result.Size = new Size(1683, 1189);
            richTextBox_result.TabIndex = 3;
            richTextBox_result.Text = "";
            // 
            // textBox_write
            // 
            textBox_write.Location = new Point(237, 148);
            textBox_write.Multiline = true;
            textBox_write.Name = "textBox_write";
            textBox_write.Size = new Size(218, 50);
            textBox_write.TabIndex = 4;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Microsoft YaHei UI", 12F);
            label1.Location = new Point(92, 157);
            label1.Name = "label1";
            label1.Size = new Size(122, 41);
            label1.TabIndex = 5;
            label1.Text = "写入值:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Microsoft YaHei UI", 12F);
            label2.Location = new Point(78, 234);
            label2.Name = "label2";
            label2.Size = new Size(154, 41);
            label2.TabIndex = 7;
            label2.Text = "写入地址:";
            // 
            // textBox_address
            // 
            textBox_address.Location = new Point(237, 228);
            textBox_address.Multiline = true;
            textBox_address.Name = "textBox_address";
            textBox_address.Size = new Size(218, 50);
            textBox_address.TabIndex = 6;
            // 
            // button_write
            // 
            button_write.Location = new Point(536, 157);
            button_write.Name = "button_write";
            button_write.Size = new Size(284, 101);
            button_write.TabIndex = 8;
            button_write.Text = "写入";
            button_write.UseVisualStyleBackColor = true;
            button_write.Click += button_write_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Microsoft YaHei UI", 12F);
            label3.Location = new Point(959, 157);
            label3.Name = "label3";
            label3.Size = new Size(154, 41);
            label3.TabIndex = 10;
            label3.Text = "读取地址:";
            // 
            // textBox_read_address
            // 
            textBox_read_address.Location = new Point(1104, 148);
            textBox_read_address.Multiline = true;
            textBox_read_address.Name = "textBox_read_address";
            textBox_read_address.Size = new Size(218, 50);
            textBox_read_address.TabIndex = 9;
            // 
            // button_read
            // 
            button_read.Location = new Point(1038, 228);
            button_read.Name = "button_read";
            button_read.Size = new Size(284, 101);
            button_read.TabIndex = 11;
            button_read.Text = "读取";
            button_read.UseVisualStyleBackColor = true;
            button_read.Click += button_read_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(14F, 31F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1702, 1641);
            Controls.Add(button_read);
            Controls.Add(label3);
            Controls.Add(textBox_read_address);
            Controls.Add(button_write);
            Controls.Add(label2);
            Controls.Add(textBox_address);
            Controls.Add(label1);
            Controls.Add(textBox_write);
            Controls.Add(richTextBox_result);
            Controls.Add(button_disconnect);
            Controls.Add(button_connect);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button_connect;
        private Button button_disconnect;
        private RichTextBox richTextBox_result;
        private TextBox textBox_write;
        private Label label1;
        private Label label2;
        private TextBox textBox_address;
        private Button button_write;
        private Label label3;
        private TextBox textBox_read_address;
        private Button button_read;
    }
}
