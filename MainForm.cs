using Modbus.Device;
using Modbus.Extensions.Enron;
using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModbusAddressScanner
{
    /// <summary>
    /// 2025年11月18日，由豆包(https://www.doubao.com/)生成，只修改部分界面布局和添加依赖。
    /// </summary>
    public partial class MainForm : Form
    {
        // 串口相关变量
        private System.IO.Ports.SerialPort _serialPort;
        private ModbusSerialMaster _modbusMaster;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isScanning;
        //发送读modbus寄存器指令中的起始地址和读寄存器个数
        private int readRegistersStart;
        private int readRegistersCount;
        public MainForm()
        {
            InitializeComponent();
            InitializeSerialPortOptions();
            InitializeControls();
        }

        #region 初始化
        /// <summary>
        /// 初始化串口选项（端口号、波特率等）
        /// </summary>
        private void InitializeSerialPortOptions()
        {
            // 加载可用串口
            cboPort.Items.AddRange(SerialPort.GetPortNames());
            if (cboPort.Items.Count > 0)
                cboPort.SelectedIndex = 0;

            // 波特率选项
            cboBaudRate.Items.AddRange(new object[] { 9600, 19200, 38400, 57600, 115200 });
            cboBaudRate.SelectedItem = 9600;

            // 数据位选项
            cboDataBits.Items.AddRange(new object[] { 7, 8 });
            cboDataBits.SelectedItem = 8;

            // 校验位选项
            cboParity.Items.AddRange(Enum.GetNames(typeof(Parity)));
            cboParity.SelectedItem = Parity.None.ToString();

            // 停止位选项
            cboStopBits.Items.AddRange(Enum.GetNames(typeof(StopBits)));
            cboStopBits.SelectedItem = StopBits.One.ToString();
        }

        /// <summary>
        /// 初始化控件状态
        /// </summary>
        private void InitializeControls()
        {
            btnStop.Enabled = false;
            txtLog.ReadOnly = true;
            _isScanning = false;
        }
        #endregion

        #region 串口操作
        /// <summary>
        /// 初始化串口和Modbus主机
        /// </summary>
        /// <returns>是否初始化成功</returns>
        private bool InitSerialPort()
        {
            try
            {
                // 关闭已打开的串口
                if (_serialPort != null && _serialPort.IsOpen)
                    _serialPort.Close();

                // 创建新串口实例
                _serialPort = new SerialPort(
                    cboPort.SelectedItem.ToString(),
                    int.Parse(cboBaudRate.SelectedItem.ToString()),
                    (Parity)Enum.Parse(typeof(Parity), cboParity.SelectedItem.ToString()),
                    int.Parse(cboDataBits.SelectedItem.ToString()),
                    (StopBits)Enum.Parse(typeof(StopBits), cboStopBits.SelectedItem.ToString())
                );

                // 串口参数配置
                _serialPort.Handshake = Handshake.None;
                _serialPort.ReadTimeout = 1000; // 读取超时1秒
                _serialPort.WriteTimeout = 1000; // 写入超时1秒

                // 打开串口
                _serialPort.Open();

                // 创建Modbus RTU主机
                _modbusMaster = ModbusSerialMaster.CreateRtu(_serialPort);
                _modbusMaster.Transport.ReadTimeout = 1000;
                _modbusMaster.Transport.Retries = 0; // 不重试，提高扫描效率

                Log($"串口初始化成功：{_serialPort.PortName}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"串口初始化失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 关闭串口
        /// </summary>
        private void CloseSerialPort()
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                    Log("串口已关闭");
                }
            }
            catch (Exception ex)
            {
                Log($"关闭串口失败：{ex.Message}");
            }
        }
        #endregion

        #region Modbus扫描逻辑
        /// <summary>
        /// 开始地址扫描（异步执行，避免阻塞UI）
        /// </summary>
        private async void StartScan()
        {
            // 先校验寄存器参数再打开串口：否则参数错误时串口已被 Open() 却直接 return，
            // 会一直占用串口句柄直到下次扫描或关闭程序
            if (!checkRegisterParam())
            {
                Log("寄存器参数有误，请检查");
                return;
            }
            if (!InitSerialPort())
                return;
            _isScanning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            // 更新UI状态
            UpdateControlStates(false);
            Log("开始扫描Modbus地址（0-254）...");

            try
            {
                // 地址从0到254递增扫描
                for (byte address = 0; address <= 254; address++)
                {
                    if (token.IsCancellationRequested)
                    {
                        Log("扫描已取消");
                        break;
                    }

                    Log($"正在扫描地址：{address}");

                    // 发送Modbus指令（读取保持寄存器，示例：地址0，长度1）
                    bool hasResponse = await SendModbusRequest(address, token);

                    if (hasResponse)
                    {
                        // 收到返回，停止扫描
                        Log($"✅ 地址 {address} 响应成功！扫描停止");
                        MessageBox.Show($"找到有效Modbus地址：{address}", "扫描成功",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    }

                    // 扫描间隔（可调整，避免发送过快）
                    await Task.Delay(100, token);
                }

                if (!token.IsCancellationRequested && _isScanning)
                {
                    Log("❌ 扫描完成，未找到有效地址");
                    MessageBox.Show("所有地址扫描完毕，未收到任何响应", "扫描完成",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Log($"扫描异常：{ex.Message}");
            }
            finally
            {
                // 清理资源
                _isScanning = false;
                CloseSerialPort();
                UpdateControlStates(true);
            }
        }
        
        /// <summary>
        /// 检查是否正确输入需要读取寄存器起始地址和读取寄存器数量
        /// </summary>
        /// <returns>输入参数是否正常</returns>
        private bool checkRegisterParam()
        {
            try
            {
                readRegistersStart = int.Parse(tb_readRegisterStart.Text.Trim());
                readRegistersCount = int.Parse(tb_readRegisterCount.Text.Trim());
                return true;
            }
            catch (Exception ex)
            {
                Log($"参数错误：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送Modbus请求并检查响应
        /// </summary>
        /// <param name="slaveAddress">从站地址</param>
        /// <param name="token">取消令牌</param>
        /// <returns>是否收到响应</returns>
        private async Task<bool> SendModbusRequest(byte slaveAddress, CancellationToken token)
        {
            try
            {
                // 示例：读取保持寄存器（功能码0x03），寄存器地址0，读取1个寄存器
                // 可根据实际需求修改功能码和参数
                var result = await Task.Run(() =>
                {
                    return _modbusMaster.ReadHoldingRegisters(
                        slaveAddress, // 从站地址
                        (ushort)readRegistersStart,            // 寄存器起始地址,1004-固件版本号
                        (ushort)readRegistersCount             // 读取寄存器数量
                    );
                }, token);

                // 有返回结果则表示响应成功
                return result != null && result.Length > 0;
            }
            catch (Modbus.SlaveException ex)
            {
                // 从机返回异常响应帧（功能码 | 0x80）说明该从机真实存在，
                // 只是拒绝了本次请求（典型情况：请求的寄存器地址不存在）。
                // 这是一个带合法CRC的完整应答，同样应判定为"发现设备"。
                Log($"✅ 地址 {slaveAddress} 存在（从机返回异常码 0x{(byte)ex.SlaveExceptionCode:X2}）");
                return true;
            }
            catch (Exception ex)
            {
                // 读超时、CRC校验失败等才会走到这里，说明该地址确实无响应
                Log($"地址 {slaveAddress} 无响应：{ex.Message.Split('\n')[0]}");
                return false;
            }
        }

        /// <summary>
        /// 停止扫描
        /// </summary>
        private void StopScan()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                _isScanning = false;
                Log("正在停止扫描...");
            }
        }
        #endregion

        #region UI辅助方法
        /// <summary>
        /// 记录日志（线程安全）
        /// </summary>
        /// <param name="message">日志内容</param>
        private void Log(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => Log(message)));
                return;
            }

            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            txtLog.ScrollToCaret();
        }

        /// <summary>
        /// 更新控件状态
        /// </summary>
        /// <param name="isIdle">是否处于空闲状态</param>
        private void UpdateControlStates(bool isIdle)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool>(UpdateControlStates), isIdle);
                return;
            }

            // 扫描时禁用配置项和开始按钮，启用停止按钮
            cboPort.Enabled = isIdle;
            cboBaudRate.Enabled = isIdle;
            cboDataBits.Enabled = isIdle;
            cboParity.Enabled = isIdle;
            cboStopBits.Enabled = isIdle;
            btnStart.Enabled = isIdle;
            btnStop.Enabled = !isIdle;
        }
        #endregion

        #region 控件事件
        private void btnStart_Click(object sender, EventArgs e)
        {
            // 清空日志
            txtLog.Clear();
            // 开始扫描
            StartScan();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            StopScan();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 关闭窗口时停止扫描并关闭串口
            if (_isScanning)
                StopScan();

            CloseSerialPort();
        }
        #endregion
    }
}