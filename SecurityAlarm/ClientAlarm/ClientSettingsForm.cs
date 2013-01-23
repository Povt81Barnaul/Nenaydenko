using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using SecurityAlarmLibrary;

using AForge.Video.DirectShow;
using AForge.Vision.Motion;

namespace ClientAlarm
{
    public partial class ClientSettingsForm : Form
    {

        //Адрес сервера
        private IPEndPoint serverAddress;
        //Клиент
        private Client client;
        //Флаг закрытия
        private bool isAppExit;

        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoCamera;
        private MotionDetector detector;

        public ClientSettingsForm()
        {
            InitializeComponent();
            this.Opacity = 0;
            this.WindowState = FormWindowState.Minimized;
            this.isAppExit = false;
        }

        //Создание клиента
        private void ClientSettingsFormExtracted()
        {
            //Загрузка настроек
            textBox1.Text = global::ClientAlarm.Properties.Settings.Default.SERVER_IP_ADDRESS;
            textBox2.Text = global::ClientAlarm.Properties.Settings.Default.SERVER_PORT;

            client = null;
            serverAddress = null;
            try
            {
                serverAddress = Utils.isValidAddress(textBox1.Text, textBox2.Text);
                if (serverAddress == null)
                    throw new FormatException("Не правильный формат ip адреса или порта");
                client = new Client(serverAddress);
                if (videoCamera.IsRunning)
                    videoCamera.Stop();
                client.VideoCamera = this.videoCamera;
                client.Detector = this.detector;
            }
            catch (FormatException fexp)
            {
                MessageBox.Show(fexp.Message);
            }
        }
        //Настройка программы
        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;
            this.Activate();
            this.Opacity = 100;
        }

        //Выход из программы
        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (videoCamera!=null && videoCamera.IsRunning)
                videoCamera.Stop();
            client.CloseClientCmd();
            this.Dispose();
        }

        //Сворачивание в трей при нажатии на крестик
        private void ClientMainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!isAppExit)
                e.Cancel = true;
            else
                e.Cancel = false;
            
            this.Opacity = 0;
            this.WindowState = FormWindowState.Minimized;
        }

        //Разворот формы
        private void ClientMainForm_MaximumSizeChanged(object sender, EventArgs e)
        {
            //Загрузка настроек
            textBox1.Text = global::ClientAlarm.Properties.Settings.Default.SERVER_IP_ADDRESS;
            textBox2.Text = global::ClientAlarm.Properties.Settings.Default.SERVER_PORT;
        }

        //Сохранить настройки
        private void button1_Click(object sender, EventArgs e)
        {
            //Проверим на валидность введенные значения
            try
            {
                string ip = textBox1.Text.Trim();
                string port = textBox2.Text.Trim();

                Regex regex = new Regex(Utils.PATTERN_IP);
                Match match = regex.Match(ip);
                if (!match.Success)
                    throw new FormatException("Не правильный формат ip адреса");

                regex = new Regex(Utils.PATTERN_PORT);
                match = regex.Match(port);
                if (!match.Success)
                    throw new FormatException("Не правильный формат порта");

                IPEndPoint server = Utils.isValidAddress(ip, port);
                if (server == null)
                    throw new FormatException("Не правильный формат ip адреса или порта");  
                
                serverAddress = server;

            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
                return;
            }
            //Сохраняем настройки в файл настроек
            global::ClientAlarm.Properties.Settings.Default.SERVER_IP_ADDRESS = serverAddress.Address.ToString();
            global::ClientAlarm.Properties.Settings.Default.SERVER_PORT = serverAddress.Port.ToString();
            global::ClientAlarm.Properties.Settings.Default.Save();

            MessageBox.Show("Настройки успешно сохранены! Программа будет перезапущенна!");
            if (videoCamera.IsRunning)
                videoCamera.Stop();
            client.CloseClientCmd();
            Dispose();
            Application.Restart();
        }

        //Не сохронять настройки
        private void button2_Click(object sender, EventArgs e)
        {
            global::ClientAlarm.Properties.Settings.Default.Reload();
            Close();
        }

        //Показать запись с камеры
        private void videoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ClientShowVideoForm csvf = new ClientShowVideoForm(this.videoCamera))
            {
                csvf.ShowDialog();
            }
        }

        //Загрузка формы
        private void ClientSettingsForm_Load(object sender, EventArgs e)
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (videoDevices.Count == 0)
            {
                MessageBox.Show("Камера не найдена!");
                isAppExit = true;
                Application.Exit();
            }

            videoCamera = new VideoCaptureDevice(videoDevices[0].MonikerString);
            detector = new MotionDetector(new SimpleBackgroundModelingDetector(),
                new MotionAreaHighlighting());

            if (this.videoCamera.IsRunning)
                this.videoCamera.Stop();

            videoCamera.NewFrame += delegate(object send, AForge.Video.NewFrameEventArgs eventArgs)
            {
                //Отлавливаем движение
                if (detector.ProcessFrame(eventArgs.Frame) > 0.02)
                {
                    if (client == null)
                        return;

                    client.SignalAlarmCmd();
                }
            };

            ClientSettingsFormExtracted();
        }
    }
}
