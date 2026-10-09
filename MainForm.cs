using Modbus.Device;
using Modbus.Extensions.Enron;
using System;
using System.Diagnostics;
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
        private System.IO.Ports.SerialPort? _serialPort;
        private ModbusSerialMaster? _modbusMaster;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isScanning;
        //发送读modbus寄存器指令中的起始地址和读寄存器个数
        private int readRegistersStart;
        private int readRegistersCount;
        // 手动指定的读超时（毫秒），null 表示界面留空、按波特率自动计算
        private int? manualReadTimeoutMs;
        // 本次扫描要依次尝试的读功能码（1=线圈 2=离散输入 3=保持寄存器 4=输入寄存器）
        private List<byte> selectedFunctionCodes = new List<byte>();
        // 功能码列表的显示文本，如 "01/03"
        private string selectedFunctionCodeText = string.Empty;
        // 本次扫描实际生效的读超时（毫秒），用于开始扫描时估算耗时
        private int effectiveReadTimeoutMs;

        // 扫描的从机地址范围：地址 0 为广播地址（从机不应答），248~255 为保留地址
        private const byte ScanAddressStart = 1;
        private const byte ScanAddressEnd = 247;
        // 单次探测中除读超时之外的固定开销（发送请求、串口读写、CRC 校验等）约 30ms，
        // 计入后预计时间更贴近实际，避免系统性低估
        private const int PerProbeOverheadMs = 30;
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

            // 读超时留空即按波特率自动计算
            tb_readTimeout.PlaceholderText = "自动";
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
                // 释放上一次扫描遗留的串口与主机资源
                CloseSerialPort();

                int baudRate = int.Parse(cboBaudRate.SelectedItem!.ToString()!);

                // 创建新串口实例
                _serialPort = new SerialPort(
                    cboPort.SelectedItem!.ToString()!,
                    baudRate,
                    (Parity)Enum.Parse(typeof(Parity), cboParity.SelectedItem!.ToString()!),
                    int.Parse(cboDataBits.SelectedItem!.ToString()!),
                    (StopBits)Enum.Parse(typeof(StopBits), cboStopBits.SelectedItem!.ToString()!)
                );

                // 串口参数配置
                _serialPort.Handshake = Handshake.None;

                // 读超时优先取界面手动设置的值，留空时按波特率估算。
                // 固定 1 秒并不合适：高波特率下绝大部分等待是浪费（扫描耗时被放大数倍），
                // 低波特率下又必须留够整帧的传输时间，否则会漏掉响应偏慢的从机
                int autoReadTimeout = CalcReadTimeout(baudRate);
                int readTimeout = manualReadTimeoutMs ?? autoReadTimeout;

                // 手动值低于自动建议值时给出提醒：可能从机还没来得及应答就被判为无响应
                if (manualReadTimeoutMs.HasValue && manualReadTimeoutMs.Value < autoReadTimeout)
                {
                    Log($"提示：手动设置的读超时 {manualReadTimeoutMs.Value} ms 低于当前波特率的建议值 {autoReadTimeout} ms，" +
                        "响应较慢的从机可能被漏掉");
                }

                _serialPort.ReadTimeout = readTimeout;
                _serialPort.WriteTimeout = 1000; // 写入超时1秒
                effectiveReadTimeoutMs = readTimeout;

                // 打开串口
                _serialPort.Open();

                // 创建Modbus RTU主机
                _modbusMaster = ModbusSerialMaster.CreateRtu(_serialPort);
                _modbusMaster.Transport.ReadTimeout = readTimeout;
                _modbusMaster.Transport.Retries = 0; // 不重试，提高扫描效率

                Log($"串口初始化成功：{_serialPort.PortName}（波特率 {baudRate}，读超时 {readTimeout} ms" +
                    $"{(manualReadTimeoutMs.HasValue ? "，手动设置" : "，自动计算")}）");
                return true;
            }
            catch (Exception ex)
            {
                Log($"串口初始化失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 按波特率估算单次读操作的合理超时时间
        /// </summary>
        /// <param name="baudRate">波特率</param>
        /// <returns>读超时毫秒数</returns>
        private static int CalcReadTimeout(int baudRate)
        {
            // 最坏响应帧约 256 字节（读125个寄存器），串口每字节 11 位
            // （1起始位 + 8数据位 + 1校验位 + 1停止位）
            double frameMs = 256.0 * 11.0 * 1000.0 / baudRate;

            // 加 200ms 从机处理余量；下限 300ms 避免偶发干扰造成误判，
            // 上限 2000ms 兜底异常波特率
            return Math.Clamp((int)frameMs + 200, 300, 2000);
        }

        /// <summary>
        /// 将秒数格式化为便于阅读的时长文本
        /// </summary>
        /// <param name="seconds">秒数</param>
        private static string FormatDuration(double seconds)
        {
            if (seconds < 60)
                return $"{seconds:F1} 秒";

            int totalSeconds = (int)Math.Round(seconds);
            return $"{totalSeconds / 60} 分 {totalSeconds % 60} 秒";
        }

        /// <summary>
        /// 关闭并释放串口与Modbus主机资源
        /// </summary>
        private void CloseSerialPort()
        {
            bool wasOpen = false;
            try
            {
                wasOpen = _serialPort != null && _serialPort.IsOpen;
            }
            catch
            {
                // 串口可能已被释放，忽略
            }

            // ModbusSerialMaster 实现了 IDisposable，且内部持有串口，必须先释放
            try
            {
                _modbusMaster?.Dispose();
            }
            catch (Exception ex)
            {
                Log($"释放Modbus主机失败：{ex.Message}");
            }
            finally
            {
                _modbusMaster = null;
            }

            // 只调用 Close() 不会释放 SerialPort 内部的流与事件轮询线程，必须 Dispose()
            try
            {
                _serialPort?.Dispose();
            }
            catch (Exception ex)
            {
                Log($"释放串口失败：{ex.Message}");
            }
            finally
            {
                _serialPort = null;
            }

            if (wasOpen)
                Log("串口已关闭");
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
            if (!checkReadTimeoutParam())
                return;
            if (!checkScanFunctionParam())
                return;
            if (!InitSerialPort())
                return;
            _isScanning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            // 更新UI状态
            UpdateControlStates(false);
            Log($"开始扫描Modbus地址（{ScanAddressStart}-{ScanAddressEnd}），读功能码：{selectedFunctionCodeText}");

            // 预计耗时：只有无响应的地址才会等满读超时，且每个已勾选的功能码各等一轮，
            // 因此按最坏情况（所有地址都无响应）给出上界
            int addressCount = ScanAddressEnd - ScanAddressStart + 1;
            double worstSeconds = addressCount * (effectiveReadTimeoutMs + PerProbeOverheadMs) / 1000.0
                                  * selectedFunctionCodes.Count;
            Log($"预计扫描时间：最坏约 {FormatDuration(worstSeconds)}"
                + $"（{addressCount} 个地址 × 读超时 {effectiveReadTimeoutMs} ms × {selectedFunctionCodes.Count} 个功能码，"
                + "找到设备的地址会明显更快）");

            Stopwatch stopwatch = Stopwatch.StartNew();
            int scannedCount = 0;
            // 收集扫描到的所有从机地址（含命中的功能码）：发现设备后不停止，继续扫描完整个地址段
            var foundAddresses = new List<string>();

            try
            {
                // 地址 0 为广播地址（从机不应答），248~255 为保留地址，
                // 因此只在 ScanAddressStart ~ ScanAddressEnd 这段合法从机地址内扫描
                for (byte address = ScanAddressStart; address <= ScanAddressEnd; address++)
                {
                    if (token.IsCancellationRequested)
                    {
                        Log(foundAddresses.Count > 0
                            ? $"扫描已取消，已发现的从机地址：{string.Join("、", foundAddresses)}"
                            : "扫描已取消");
                        break;
                    }

                    // 只在第一个地址失败时记录异常原因，便于排查串口/接线问题；
                    // 其余地址不再逐条记录，避免刷屏
                    byte? matchedFunction = await SendModbusRequest(address, token, scannedCount == 0);
                    scannedCount++;

                    if (matchedFunction.HasValue)
                    {
                        // 找到设备后不停止扫描，记录地址与命中的功能码，继续往下扫描，最后统一汇总
                        foundAddresses.Add($"{address}（功能码{matchedFunction.Value:D2}）");
                        Log($"✅ 发现从机地址：{address}（功能码 {matchedFunction.Value:D2}，累计 {foundAddresses.Count} 个，继续扫描...）");
                    }

                    // 每 10 个地址输出一次进度即可（247 个地址共 24 行）。
                    // 原先每个地址输出 2 行日志、247 个地址共 500+ 行，
                    // 且每行都执行一次 ScrollToCaret 触发重绘，严重拖慢扫描速度。
                    // 这里也不需要额外的扫描间隔：每次探测失败都已经等待了完整的读超时，
                    // 总线上的空闲时间远超 Modbus 要求的 3.5 个字符时间
                    if (address % 10 == 0)
                        Log($"[进度] 已扫描 {address}/{ScanAddressEnd}，已发现 {foundAddresses.Count} 个从机...");
                }

                if (!token.IsCancellationRequested && _isScanning)
                {
                    double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

                    if (foundAddresses.Count > 0)
                    {
                        string addressList = string.Join("、", foundAddresses);
                        Log($"✅ 扫描完成，共找到 {foundAddresses.Count} 个从机地址：{addressList}（耗时 {elapsedSeconds:F1} 秒）");
                        MessageBox.Show(
                            $"共找到 {foundAddresses.Count} 个有效Modbus地址：\n\n{addressList}\n\n扫描耗时 {elapsedSeconds:F1} 秒",
                            "扫描完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        Log($"❌ 扫描完成，未找到有效地址（共探测 {scannedCount} 个地址，耗时 {elapsedSeconds:F1} 秒）");
                        MessageBox.Show("所有地址扫描完毕，未收到任何响应", "扫描完成",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log("扫描已取消");
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
                // CancellationTokenSource 同样实现了 IDisposable，每次扫描新建，必须释放
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                UpdateControlStates(true);
            }
        }
        
        /// <summary>
        /// 检查是否正确输入需要读取寄存器起始地址和读取寄存器数量
        /// </summary>
        /// <returns>输入参数是否正常</returns>
        private bool checkRegisterParam()
        {
            if (!int.TryParse(tb_readRegisterStart.Text.Trim(), out readRegistersStart))
            {
                Log("参数错误：寄存器起始地址必须是 0 ~ 65535 之间的整数");
                return false;
            }

            if (readRegistersStart < 0 || readRegistersStart > 65535)
            {
                Log($"参数错误：寄存器起始地址 {readRegistersStart} 超出范围（0 ~ 65535）");
                return false;
            }

            if (!int.TryParse(tb_readRegisterCount.Text.Trim(), out readRegistersCount))
            {
                Log("参数错误：寄存器数量必须是 1 ~ 125 之间的整数");
                return false;
            }

            // Modbus 功能码 0x03 单次最多读取 125 个寄存器
            if (readRegistersCount < 1 || readRegistersCount > 125)
            {
                Log($"参数错误：寄存器数量 {readRegistersCount} 超出范围（1 ~ 125）");
                return false;
            }

            if (readRegistersStart + readRegistersCount > 65536)
            {
                Log($"参数错误：起始地址与数量之和 {readRegistersStart + readRegistersCount} 超出 65535 上限");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 校验读超时设置：界面留空表示按波特率自动计算
        /// </summary>
        /// <returns>输入参数是否正常</returns>
        private bool checkReadTimeoutParam()
        {
            string text = tb_readTimeout.Text.Trim();

            // 留空 -> 按波特率自动计算
            if (text.Length == 0)
            {
                manualReadTimeoutMs = null;
                return true;
            }

            if (!int.TryParse(text, out int readTimeout))
            {
                Log("参数错误：读超时必须是整数（单位：毫秒），留空表示按波特率自动计算");
                return false;
            }

            if (readTimeout < 1 || readTimeout > 60000)
            {
                Log($"参数错误：读超时 {readTimeout} ms 超出范围（1 ~ 60000 毫秒）");
                return false;
            }

            manualReadTimeoutMs = readTimeout;
            return true;
        }

        /// <summary>
        /// 读取界面上已勾选的读功能码
        /// </summary>
        /// <returns>功能码列表（1=线圈 2=离散输入 3=保持寄存器 4=输入寄存器）</returns>
        private List<byte> GetSelectedFunctionCodes()
        {
            var functionCodes = new List<byte>();

            if (chkFunc01.Checked)
                functionCodes.Add(1);
            if (chkFunc02.Checked)
                functionCodes.Add(2);
            if (chkFunc03.Checked)
                functionCodes.Add(3);
            if (chkFunc04.Checked)
                functionCodes.Add(4);

            return functionCodes;
        }

        /// <summary>
        /// 校验读功能码的勾选情况：至少要勾选一个
        /// </summary>
        /// <returns>输入参数是否正常</returns>
        private bool checkScanFunctionParam()
        {
            selectedFunctionCodes = GetSelectedFunctionCodes();

            if (selectedFunctionCodes.Count == 0)
            {
                Log("参数错误：请至少勾选一个读功能码（01 线圈 / 02 离散输入 / 03 保持寄存器 / 04 输入寄存器）");
                return false;
            }

            selectedFunctionCodeText = string.Join("/", selectedFunctionCodes.Select(fc => $"{fc:D2}"));
            return true;
        }

        /// <summary>
        /// 对指定从站地址依次尝试界面上所有已勾选的读功能码，任意一个收到响应即认为该地址存在
        /// </summary>
        /// <param name="slaveAddress">从站地址</param>
        /// <param name="token">取消令牌</param>
        /// <param name="logFailure">无响应时是否输出日志（默认关闭，避免 247 个地址刷屏）</param>
        /// <returns>命中的功能码；null 表示所有已勾选的功能码都没有响应</returns>
        private async Task<byte?> SendModbusRequest(byte slaveAddress, CancellationToken token, bool logFailure = false)
        {
            // 取本地引用，避免串口已被释放时在后台线程访问到 null 字段
            var master = _modbusMaster;
            if (master == null)
                return null;

            ushort startAddress = (ushort)readRegistersStart;
            ushort pointCount = (ushort)readRegistersCount;
            string? firstError = null;

            foreach (byte functionCode in selectedFunctionCodes)
            {
                try
                {
                    bool hasData = await Task.Run(() =>
                    {
                        // 01/02 读取位数据（bool[]），03/04 读取字数据（ushort[]）
                        return functionCode switch
                        {
                            1 => master.ReadCoils(slaveAddress, startAddress, pointCount) is { Length: > 0 },
                            2 => master.ReadInputs(slaveAddress, startAddress, pointCount) is { Length: > 0 },
                            3 => master.ReadHoldingRegisters(slaveAddress, startAddress, pointCount) is { Length: > 0 },
                            4 => master.ReadInputRegisters(slaveAddress, startAddress, pointCount) is { Length: > 0 },
                            _ => false
                        };
                    }, token);

                    // 有返回数据表示该功能码响应成功
                    if (hasData)
                        return functionCode;
                }
                catch (OperationCanceledException)
                {
                    // 任务尚未开始执行就被取消，交由 StartScan 统一处理，不要当作"无响应"
                    throw;
                }
                catch (Modbus.SlaveException ex)
                {
                    // 从机返回异常响应帧（功能码 | 0x80）说明该从机真实存在，
                    // 只是拒绝了本次请求（典型情况：该功能码不支持，或请求的地址不存在）。
                    // 这是一个带合法CRC的完整应答，同样应判定为"发现设备"。
                    Log($"✅ 地址 {slaveAddress} 存在（功能码 {functionCode:D2} 返回异常码 0x{(byte)ex.SlaveExceptionCode:X2}）");
                    return functionCode;
                }
                catch (Exception ex)
                {
                    // 读超时、CRC校验失败等才会走到这里，说明该功能码没有响应
                    firstError ??= ex.Message.Split('\n')[0];
                }
            }

            // 所有已勾选的功能码都没有响应：只在需要时记录一次，便于排查串口/接线问题
            if (logFailure && firstError != null)
                Log($"地址 {slaveAddress} 无响应（已尝试功能码 {selectedFunctionCodeText}）：{firstError}");

            return null;
        }

        /// <summary>
        /// 停止扫描
        /// </summary>
        private void StopScan()
        {
            // 取本地引用，避免在 Cancel() 与释放之间出现竞争
            var cts = _cancellationTokenSource;
            if (cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel();
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
            // 窗体正在关闭或已释放时直接丢弃日志，避免抛 ObjectDisposedException
            if (IsDisposed || !IsHandleCreated)
                return;

            if (txtLog.InvokeRequired)
            {
                // 用 BeginInvoke 异步投递，避免关闭过程中同步等待造成死锁
                txtLog.BeginInvoke(new Action(() => Log(message)));
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
            if (IsDisposed || !IsHandleCreated)
                return;

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