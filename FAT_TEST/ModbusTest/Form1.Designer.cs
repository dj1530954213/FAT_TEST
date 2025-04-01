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
            textBox_pc = new TextBox();
            label1 = new Label();
            button_start = new Button();
            label2 = new Label();
            textBox_sd = new TextBox();
            button_stop = new Button();
            label3 = new Label();
            textBox_batch = new TextBox();
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
            // textBox_pc
            // 
            textBox_pc.Location = new Point(240, 133);
            textBox_pc.Multiline = true;
            textBox_pc.Name = "textBox_pc";
            textBox_pc.Size = new Size(200, 59);
            textBox_pc.TabIndex = 4;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            label1.Location = new Point(27, 151);
            label1.Name = "label1";
            label1.Size = new Size(178, 41);
            label1.TabIndex = 5;
            label1.Text = "偏差值设定";
            // 
            // button_start
            // 
            button_start.Location = new Point(541, 136);
            button_start.Name = "button_start";
            button_start.Size = new Size(177, 77);
            button_start.TabIndex = 6;
            button_start.Text = "开始同步";
            button_start.UseVisualStyleBackColor = true;
            button_start.Click += button_start_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            label2.Location = new Point(27, 219);
            label2.Name = "label2";
            label2.Size = new Size(178, 41);
            label2.TabIndex = 8;
            label2.Text = "锁定状态量";
            // 
            // textBox_sd
            // 
            textBox_sd.Location = new Point(240, 219);
            textBox_sd.Multiline = true;
            textBox_sd.Name = "textBox_sd";
            textBox_sd.Size = new Size(200, 59);
            textBox_sd.TabIndex = 7;
            // 
            // button_stop
            // 
            button_stop.Location = new Point(541, 219);
            button_stop.Name = "button_stop";
            button_stop.Size = new Size(177, 77);
            button_stop.TabIndex = 9;
            button_stop.Text = "停止同步";
            button_stop.UseVisualStyleBackColor = true;
            button_stop.Click += button_stop_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            label3.Location = new Point(27, 314);
            label3.Name = "label3";
            label3.Size = new Size(82, 41);
            label3.TabIndex = 11;
            label3.Text = "批次";
            // 
            // textBox_batch
            // 
            textBox_batch.Location = new Point(240, 314);
            textBox_batch.Multiline = true;
            textBox_batch.Name = "textBox_batch";
            textBox_batch.Size = new Size(200, 59);
            textBox_batch.TabIndex = 10;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(14F, 31F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1702, 1641);
            Controls.Add(label3);
            Controls.Add(textBox_batch);
            Controls.Add(button_stop);
            Controls.Add(label2);
            Controls.Add(textBox_sd);
            Controls.Add(button_start);
            Controls.Add(label1);
            Controls.Add(textBox_pc);
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
        private TextBox textBox_pc;
        private Label label1;
        private Button button_start;
        private Label label2;
        private TextBox textBox_sd;
        private Button button_stop;
        private Label label3;
        private TextBox textBox_batch;
    }
}
