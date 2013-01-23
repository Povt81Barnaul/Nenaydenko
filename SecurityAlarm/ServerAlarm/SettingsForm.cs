using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using SecurityAlarmLibrary;
using System.Net.Sockets;

namespace ServerAlarm
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
        }

        //Сохранить
        private void button1_Click(object sender, EventArgs e)
        {
            String ip = null;
            String port = null;
            try
            {
                if (comboBox1.SelectedIndex < 0)
                    throw new Exception("Не выбран ip адрес!");

                ip = comboBox1.SelectedItem.ToString();
                port = textBox2.Text.Trim();

                //Проверка валидности введенных данных
                IPAddress Addr = null;
                if (!IPAddress.TryParse(ip, out Addr))
                    throw new Exception("Неверный формат IP адреса");

                Regex regex = new Regex(Utils.PATTERN_PORT);
                Match match = regex.Match(port);
                if (!match.Success)
                    throw new FormatException("Не правильный формат порта");

                int Port = 0;
                if (!int.TryParse(port, out Port))
                    throw new Exception("Не правильный формат порта");

                //Сохраняем настройки в файл настроек
                global::ServerAlarm.Properties.Settings.Default.SERVER_IP_ADDRESS = Addr.ToString();
                global::ServerAlarm.Properties.Settings.Default.SERVER_PORT = Port.ToString();
                global::ServerAlarm.Properties.Settings.Default.Save();

                this.DialogResult = System.Windows.Forms.DialogResult.OK;
                MessageBox.Show("Настройки успешно сохранены! Программа будет перезапущенна!");
                
            }
            catch (Exception exp)
            {
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                MessageBox.Show(exp.Message);
            }
        }

        //Отмена
        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

        //Закрытие на крестик
        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == System.Windows.Forms.DialogResult.OK)
                return;
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        //Загрузка
        private void SettingsForm_Load(object sender, EventArgs e)
        {
            //Загрузка настроек
            string ip = global::ServerAlarm.Properties.Settings.Default.SERVER_IP_ADDRESS;
            comboBox1.Items.Clear();
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                foreach (UnicastIPAddressInformation addrInfo in nic.GetIPProperties().UnicastAddresses)
                    if (addrInfo.Address.AddressFamily == AddressFamily.InterNetwork) 
                        comboBox1.Items.Add(addrInfo.Address.ToString());

            bool isSelected = false;
            for (int i = 0; i < comboBox1.Items.Count; i++)
            {
                if (comboBox1.Items[i].ToString() == ip)
                {
                    comboBox1.SelectedItem = comboBox1.Items[i];
                    isSelected = true;
                }
            }
            textBox2.Text = global::ServerAlarm.Properties.Settings.Default.SERVER_PORT;

            if (!isSelected)
                MessageBox.Show("Выбранный ранее ip адрес сейчас недоступен! Необходимо выбрать новый!");
        }
    }
}
