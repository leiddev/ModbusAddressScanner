
namespace ModbusAddressScanner
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            gbSerialConfig = new GroupBox();
            cboStopBits = new ComboBox();
            label5 = new Label();
            cboParity = new ComboBox();
            label4 = new Label();
            cboDataBits = new ComboBox();
            label3 = new Label();
            cboBaudRate = new ComboBox();
            label2 = new Label();
            cboPort = new ComboBox();
            label1 = new Label();
            gbControl = new GroupBox();
            tb_readRegisterCount = new TextBox();
            label7 = new Label();
            tb_readRegisterStart = new TextBox();
            label6 = new Label();
            label8 = new Label();
            tb_readTimeout = new TextBox();
            btnStop = new Button();
            btnStart = new Button();
            gbLog = new GroupBox();
            txtLog = new TextBox();
            gbSerialConfig.SuspendLayout();
            gbControl.SuspendLayout();
            gbLog.SuspendLayout();
            SuspendLayout();
            // 
            // gbSerialConfig
            // 
            gbSerialConfig.Controls.Add(cboStopBits);
            gbSerialConfig.Controls.Add(label5);
            gbSerialConfig.Controls.Add(cboParity);
            gbSerialConfig.Controls.Add(label4);
            gbSerialConfig.Controls.Add(cboDataBits);
            gbSerialConfig.Controls.Add(label3);
            gbSerialConfig.Controls.Add(cboBaudRate);
            gbSerialConfig.Controls.Add(label2);
            gbSerialConfig.Controls.Add(cboPort);
            gbSerialConfig.Controls.Add(label1);
            gbSerialConfig.Controls.Add(tb_readTimeout);
            gbSerialConfig.Controls.Add(label8);
            gbSerialConfig.Location = new Point(22, 20);
            gbSerialConfig.Margin = new Padding(6, 5, 6, 5);
            gbSerialConfig.Name = "gbSerialConfig";
            gbSerialConfig.Padding = new Padding(6, 5, 6, 5);
            gbSerialConfig.Size = new Size(1320, 165);
            gbSerialConfig.TabIndex = 0;
            gbSerialConfig.TabStop = false;
            gbSerialConfig.Text = "串口配置";
            //
            // label8
            //
            label8.AutoSize = true;
            label8.Location = new Point(370, 114);
            label8.Margin = new Padding(6, 0, 6, 0);
            label8.Name = "label8";
            label8.Size = new Size(150, 28);
            label8.TabIndex = 10;
            label8.Text = "读超时(ms):";
            //
            // tb_readTimeout
            //
            tb_readTimeout.Location = new Point(545, 109);
            tb_readTimeout.Margin = new Padding(6, 5, 6, 5);
            tb_readTimeout.Name = "tb_readTimeout";
            tb_readTimeout.Size = new Size(145, 34);
            tb_readTimeout.TabIndex = 11;
            // 
            // cboStopBits
            // 
            cboStopBits.DropDownStyle = ComboBoxStyle.DropDownList;
            cboStopBits.Location = new Point(886, 58);
            cboStopBits.Margin = new Padding(6, 5, 6, 5);
            cboStopBits.Name = "cboStopBits";
            cboStopBits.Size = new Size(145, 36);
            cboStopBits.TabIndex = 9;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(787, 63);
            label5.Margin = new Padding(6, 0, 6, 0);
            label5.Name = "label5";
            label5.Size = new Size(80, 28);
            label5.TabIndex = 8;
            label5.Text = "停止位:";
            // 
            // cboParity
            // 
            cboParity.DropDownStyle = ComboBoxStyle.DropDownList;
            cboParity.Location = new Point(628, 58);
            cboParity.Margin = new Padding(6, 5, 6, 5);
            cboParity.Name = "cboParity";
            cboParity.Size = new Size(145, 36);
            cboParity.TabIndex = 7;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(529, 63);
            label4.Margin = new Padding(6, 0, 6, 0);
            label4.Name = "label4";
            label4.Size = new Size(80, 28);
            label4.TabIndex = 6;
            label4.Text = "校验位:";
            // 
            // cboDataBits
            // 
            cboDataBits.DropDownStyle = ComboBoxStyle.DropDownList;
            cboDataBits.Location = new Point(370, 58);
            cboDataBits.Margin = new Padding(6, 5, 6, 5);
            cboDataBits.Name = "cboDataBits";
            cboDataBits.Size = new Size(145, 36);
            cboDataBits.TabIndex = 5;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(271, 63);
            label3.Margin = new Padding(6, 0, 6, 0);
            label3.Name = "label3";
            label3.Size = new Size(80, 28);
            label3.TabIndex = 4;
            label3.Text = "数据位:";
            // 
            // cboBaudRate
            // 
            cboBaudRate.DropDownStyle = ComboBoxStyle.DropDownList;
            cboBaudRate.Location = new Point(111, 109);
            cboBaudRate.Margin = new Padding(6, 5, 6, 5);
            cboBaudRate.Name = "cboBaudRate";
            cboBaudRate.Size = new Size(201, 36);
            cboBaudRate.TabIndex = 3;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(13, 114);
            label2.Margin = new Padding(6, 0, 6, 0);
            label2.Name = "label2";
            label2.Size = new Size(80, 28);
            label2.TabIndex = 2;
            label2.Text = "波特率:";
            // 
            // cboPort
            // 
            cboPort.DropDownStyle = ComboBoxStyle.DropDownList;
            cboPort.Location = new Point(111, 58);
            cboPort.Margin = new Padding(6, 5, 6, 5);
            cboPort.Name = "cboPort";
            cboPort.Size = new Size(145, 36);
            cboPort.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(13, 63);
            label1.Margin = new Padding(6, 0, 6, 0);
            label1.Name = "label1";
            label1.Size = new Size(80, 28);
            label1.TabIndex = 0;
            label1.Text = "端口号:";
            // 
            // gbControl
            // 
            gbControl.Controls.Add(tb_readRegisterCount);
            gbControl.Controls.Add(label7);
            gbControl.Controls.Add(tb_readRegisterStart);
            gbControl.Controls.Add(label6);
            gbControl.Controls.Add(btnStop);
            gbControl.Controls.Add(btnStart);
            gbControl.Location = new Point(22, 194);
            gbControl.Margin = new Padding(6, 5, 6, 5);
            gbControl.Name = "gbControl";
            gbControl.Padding = new Padding(6, 5, 6, 5);
            gbControl.Size = new Size(1320, 115);
            gbControl.TabIndex = 1;
            gbControl.TabStop = false;
            gbControl.Text = "扫描控制";
            // 
            // tb_readRegisterCount
            // 
            tb_readRegisterCount.Location = new Point(730, 48);
            tb_readRegisterCount.Margin = new Padding(6, 5, 6, 5);
            tb_readRegisterCount.Name = "tb_readRegisterCount";
            tb_readRegisterCount.Size = new Size(119, 34);
            tb_readRegisterCount.TabIndex = 5;
            tb_readRegisterCount.Text = "1";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(526, 53);
            label7.Margin = new Padding(6, 0, 6, 0);
            label7.Name = "label7";
            label7.Size = new Size(180, 28);
            label7.TabIndex = 4;
            label7.Text = "读取寄存器长度：";
            // 
            // tb_readRegisterStart
            // 
            tb_readRegisterStart.Location = new Point(329, 48);
            tb_readRegisterStart.Margin = new Padding(6, 5, 6, 5);
            tb_readRegisterStart.Name = "tb_readRegisterStart";
            tb_readRegisterStart.Size = new Size(182, 34);
            tb_readRegisterStart.TabIndex = 3;
            tb_readRegisterStart.Text = "1";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(13, 53);
            label6.Margin = new Padding(6, 0, 6, 0);
            label6.Name = "label6";
            label6.Size = new Size(285, 28);
            label6.TabIndex = 2;
            label6.Text = "寄存器起始地址（十进制）：";
            // 
            // btnStop
            // 
            btnStop.Location = new Point(1094, 41);
            btnStop.Margin = new Padding(6, 5, 6, 5);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(186, 49);
            btnStop.TabIndex = 1;
            btnStop.Text = "停止扫描";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // btnStart
            // 
            btnStart.Location = new Point(886, 41);
            btnStart.Margin = new Padding(6, 5, 6, 5);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(186, 49);
            btnStart.TabIndex = 0;
            btnStart.Text = "开始扫描";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // gbLog
            // 
            gbLog.Controls.Add(txtLog);
            gbLog.Location = new Point(22, 320);
            gbLog.Margin = new Padding(6, 5, 6, 5);
            gbLog.Name = "gbLog";
            gbLog.Padding = new Padding(6, 5, 6, 5);
            gbLog.Size = new Size(1320, 362);
            gbLog.TabIndex = 2;
            gbLog.TabStop = false;
            gbLog.Text = "扫描日志";
            // 
            // txtLog
            // 
            txtLog.Dock = DockStyle.Fill;
            txtLog.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
            txtLog.Location = new Point(6, 32);
            txtLog.Margin = new Padding(6, 5, 6, 5);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(1308, 325);
            txtLog.TabIndex = 0;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(13F, 28F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1365, 702);
            Controls.Add(gbLog);
            Controls.Add(gbControl);
            Controls.Add(gbSerialConfig);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Margin = new Padding(6, 5, 6, 5);
            MaximizeBox = false;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Modbus地址扫描工具";
            FormClosing += MainForm_FormClosing;
            gbSerialConfig.ResumeLayout(false);
            gbSerialConfig.PerformLayout();
            gbControl.ResumeLayout(false);
            gbControl.PerformLayout();
            gbLog.ResumeLayout(false);
            gbLog.PerformLayout();
            ResumeLayout(false);
        }


        #endregion

        private System.Windows.Forms.GroupBox gbSerialConfig;
        private System.Windows.Forms.ComboBox cboStopBits;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cboParity;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cboDataBits;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cboBaudRate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cboPort;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox gbControl;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.GroupBox gbLog;
        private System.Windows.Forms.TextBox txtLog;
        private Label label7;
        private TextBox tb_readRegisterStart;
        private Label label6;
        private TextBox tb_readRegisterCount;
        private Label label8;
        private TextBox tb_readTimeout;
    }
}