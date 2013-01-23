using System.Net.Sockets;
using System.Threading;
using System.Net;
using System;
using System.IO;
using SecurityAlarmLibrary;
using System.Windows.Forms;
using AForge.Video.DirectShow;
using AForge.Vision.Motion;

namespace ClientAlarm
{
    /// <summary>
    /// Клиент
    /// </summary>
    public class Client : IDisposable
    {
        private const int INTERVAL_CONNECT = 5000; 

        private TcpClient _client;
        private Thread _clientThreaad;
        private IPEndPoint _serverAddress;
        private String _hostName;
        private NetworkStream _stream;
        private BinaryReader _reader;
        private BinaryWriter _writer;

        private System.Timers.Timer _timerConnect;
        private VideoCaptureDevice _videoCamera;
        private MotionDetector _detector;

        /// <summary>
        /// На сигнализации или нет?
        /// </summary>
        public bool isSignaling { get; set; }

        /// <summary>
        /// Тревога?
        /// </summary>
        public bool isAlarm { get; set; }

        /// <summary>
        /// Камера
        /// </summary>
        public VideoCaptureDevice VideoCamera
        {
            get
            {
                return _videoCamera;
            }
            set
            {
                _videoCamera = value;
            }
        }

        /// <summary>
        /// Детектор движения
        /// </summary>
        public MotionDetector Detector
        {
            get
            {
                return _detector;
            }
            set
            {
                _detector = value;
            }
        }

        /// <summary>
        /// Новый клиент
        /// </summary>
        /// <param name="serverAddress">Адрес сервера</param>
        public Client(IPEndPoint serverAddress)
        {
            _serverAddress = serverAddress;
            _hostName = Dns.GetHostName();
            _timerConnect = new System.Timers.Timer(Convert.ToDouble(INTERVAL_CONNECT));
            _timerConnect.Elapsed += _timerConnect_Elapsed;

            _clientThreaad = new Thread(RunClient) { IsBackground = true };
            _clientThreaad.Start();
        }

        //Повтор соединения по таймеру
        void _timerConnect_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_client.Connected)
                return;

            try
            {
                _client.Connect(_serverAddress);
            }
            catch (SocketException)
            {

            }
        }

        //Запуск работы клиента (в отдельном потоке принемаем данные)
        private void RunClient(object obj)
        {
            while (true)
            {
                try
                {
                    _client = new TcpClient();
                    _timerConnect.Start();

                    while (true)
                        if (_client.Connected)
                        {
                            _timerConnect.Stop();
                            break;
                        }

                    _stream = _client.GetStream();
                    _writer = new BinaryWriter(_stream);
                    _reader = new BinaryReader(_stream);

                    string helloStr = string.Format("{0} {1}", SysMessage.CONNECT, _hostName);
                    _writer.Write(helloStr);

                    try
                    {
                        bool flag = true;
                        do
                        {
                            //Считываем команды из буфера
                            string cmd = _reader.ReadString();

                            switch (cmd)
                            {
                                //Включить сигнализацию
                                case SysMessage.VC_SIGNALING:
                                    VcSignalingCmd();
                                    break;

                                //Выключить сигнализацию
                                case SysMessage.EXIT_SIGNALING:
                                    ExitSignalingCmd();
                                    break;

                                //Выключение сервера
                                case SysMessage.EXIT_SERVER:
                                    CloseResourse();
                                    flag = false;
                                    break;

                                //Выключить сигнал тревоги и продолжить наблюдение
                                case SysMessage.STOP_SIGNAL_ALARM:
                                    StopSignalAlarmCmd();
                                    break;
                            }

                        } while (flag);
                    }
                    catch (SocketException)
                    {
                        MessageBox.Show("Соединение было неожиданно разорванно!");
                        CloseResourse();
                    }
                }
                catch (Exception exp)
                {
                    CloseResourse();
                    MessageBox.Show(exp.Message);
                }
                finally
                {
                    CloseResourse();
                }
            }
        }

        //Выключить сигнал тревоги и продолжить наблюдение
        private void StopSignalAlarmCmd()
        {
            isAlarm = false;
            if (_detector != null)
                _detector.Reset();
        }

        /// <summary>
        /// Отправка на сервер сигнала тревоги
        /// </summary>
        public void SignalAlarmCmd()
        {
            if (!isSignaling)
                return;

            isAlarm = true;
            _writer.Write(SysMessage.SIGNAL_ALARM);
        }

        //Включить сигнализацию на видеокамере
        private void VcSignalingCmd()
        {
            if (_videoCamera != null)
                if (!_videoCamera.IsRunning)
                    _videoCamera.Start();

            //Если включили сигнализацию успешно отправим эту информацию серверу
            if (_videoCamera.IsRunning)
            {
                _writer.Write(SysMessage.OK_VC_SIGNALING);
                isSignaling = true;
                isAlarm = false;
            }
            else
            {
                //Иначе нужно сообщить серверу о не удаче
                _writer.Write(SysMessage.NOT_VC_SIGNALING);
                isSignaling = false;
                isAlarm = false;
            }
        }

        //Выключить сигнализацию на видеокамере
        private void ExitSignalingCmd()
        {
            if (_videoCamera != null)
                if (_videoCamera.IsRunning)
                    _videoCamera.Stop();

            //Если выключили сигнализацию успешно отправим эту информацию серверу
            if (!_videoCamera.IsRunning)
                _writer.Write(SysMessage.OK_EXIT_SIGNALING);
            else
                //Иначе нужно сообщить серверу о не удаче
                _writer.Write(SysMessage.NOT_EXIT_SIGNALING);
            isSignaling = false;
            isAlarm = false;
        }

        /// <summary>
        /// Отправка на сервер команды об отключении клиента
        /// </summary>
        public void CloseClientCmd()
        {
            if(_client != null && _client.Connected)
                _writer.Write(SysMessage.CLOSE_CONNECT);
        }

        //Закрытие ресурсов
        private void CloseResourse()
        {
            //Закрытие соединения
            _writer.Close();
            _reader.Close();
            _stream.Close();
            _client.Close();
            isAlarm = false;
            isSignaling = false;
            if (_videoCamera.IsRunning)
                _videoCamera.Stop();
        }

        //Уничтожение объекта
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        //Освобождение памяти
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_stream != null)
                {
                    _stream.Dispose();
                    _stream = null;
                }
                if (_reader != null)
                {
                    _reader.Dispose();
                    _reader = null;
                }
                if (_writer != null)
                {
                    _writer.Dispose();
                    _writer = null;
                }
                if (_timerConnect != null)
                {
                    _timerConnect.Dispose();
                    _timerConnect = null;
                }
            }
        }

        //Деструтор
        ~Client()
        {
            Dispose(false);
        }
    }
}
