using System;
using System.Net;
using System.Windows.Forms;
using SecurityAlarmLibrary;

namespace ServerAlarm
{
    public partial class MainServerForm : Form
    {
        private const int INTERVAL_UPDATE = 3000;
        
        //Сервер
        private Server server;
        //Адресс сервера
        private IPEndPoint serverAddress;
        //Флаг закрытия
        private bool isAppExit;

        private ImageList TreeImageList;

        public MainServerForm()
        {
            InitializeComponent();

            treeView1.Nodes.Add(new TreeNode("Список подключенных компьютеров"));
            TreeImageList = new ImageList();
            TreeImageList.Images.Add(global::ServerAlarm.Properties.Resources.network);
            TreeImageList.Images.Add(global::ServerAlarm.Properties.Resources.comp);
            TreeImageList.Images.Add(global::ServerAlarm.Properties.Resources.comp_activ);
            TreeImageList.Images.Add(global::ServerAlarm.Properties.Resources.comp_signal);

            treeView1.ImageList = TreeImageList;
            treeView1.ImageIndex = 0;
            treeView1.SelectedImageIndex = 0;
            this.isAppExit = false;

            LoadSettings();

            timer1.Interval = INTERVAL_UPDATE;
            timer1.Tick += UpdateTree;
        }

        private void LoadSettings()
        {
            //Загрузка настроек
            String ip = global::ServerAlarm.Properties.Settings.Default.SERVER_IP_ADDRESS;
            String port = global::ServerAlarm.Properties.Settings.Default.SERVER_PORT;

            serverAddress = null;
            try
            {
                serverAddress = Utils.isValidAddress(ip, port);
                if (serverAddress == null)
                    throw new FormatException("Не правильный формат ip адреса или порта");

                server = new Server(serverAddress);
            }
            catch (FormatException fexp)
            {
                MessageBox.Show(fexp.Message);
            }
        }
        //Обновление дерева клиентов
        void UpdateTree(object sender, EventArgs e)
        {
            TreeNode root = treeView1.Nodes[0];
            var sNode = treeView1.SelectedNode;
            server.TreeNodesUpdate(root, nodeContextMenu);
            treeView1.ExpandAll();
                        
            bool flag = false;
            foreach (TreeNode node in root.Nodes)
                if (node.Text == sNode.Text)
                {
                    treeView1.SelectedNode = node;
                    flag = true;
                }

            if(!flag)
                treeView1.SelectedNode = root;
        }

        //Загрузка программы, а вместе с ней и запуск сервера
        private void MainServerForm_Load(object sender, EventArgs e)
        {
            timer1.Start();
        }

        //Закрытие на крестик
        private void MainServerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!isAppExit)
            {
                e.Cancel = true;
                this.Opacity = 0;
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
            }
            else
                e.Cancel = false;
            Close();
        }

        //Выход (по контекстному меню)
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            CloseProgramm();
        }

        //Настройка (по контекстному меню)
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowSettingsForm();
        }

        //Открыть программу
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;
            this.Activate();
            this.Opacity = 100;
            this.ShowInTaskbar = true;
        }

        //Сворачиваем
        private void MainServerForm_MinimumSizeChanged(object sender, EventArgs e)
        {
            this.Opacity = 0;
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
        }

        //Настройка
        private void setToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSettingsForm();
        }

        //Показать форму настройки
        private void ShowSettingsForm()
        {
            bool flag = false;
            //Запуск формы настроек
            using (SettingsForm sForm = new SettingsForm())
            {
                //Если настройки изменили
                if (sForm.ShowDialog() == DialogResult.OK)
                {
                    flag = true;
                }
            }
            if (flag)
            {
                CloseProgramm();
                Application.Restart();
            }
        }

        //Завершение работы с программой
        private void CloseProgramm()
        {
            server.ExitServerCmd();
            this.Dispose();
        }

        //Выход
        private void clToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseProgramm();
        }

        //Поставить на сигнализацию
        private void onSignalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Получим выделенный хост в дереве
            TreeNode node = treeView1.SelectedNode;
            
            //Если ничего не выделенно, или выделен корневой
            if (node == null || node.Parent == null)
                //На выход
                return;

            //Иначе отправляем команду
            string hostName = node.Text;
            int id = Convert.ToInt32(node.Name);
            server.VcSignalingCmd(hostName, id);
        }

        //Снять с сигнализации
        private void notSignalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Получим выделенный хост в дереве
            TreeNode node = treeView1.SelectedNode;

            //Если ничего не выделенно, или выделен корневой
            if (node == null || node.Parent == null)
                //На выход
                return;

            //Иначе отправляем команду
            string hostName = node.Text;
            int id = Convert.ToInt32(node.Name);
            server.StopSignalAlarmCmd(hostName, id);
            server.ExitSignalingCmd(hostName, id);
        }

        //Выбор хоста в дереве
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            //Если корневой то на выход
            if (e.Node.Parent == null)
                return;

            HideMenuItems(e.Node);
        }

        //Открыть меню "Компьютер"
        private void компьютерToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView1.SelectedNode;
            if (node == null || node.Parent == null)
            {
                //Заблокируем все элементы меню
                onSignalToolStripMenuItem.Enabled = false;
                notSignalToolStripMenuItem.Enabled = false;
                stopSignalToolStripMenuItem.Enabled = false;
                return;
            }

            HideMenuItems(node);
        }

        //Скрытие кнопок меню
        private void HideMenuItems(TreeNode node)
        {
            string hostName = node.Text;
            int id = Convert.ToInt32(node.Name);
            int state = server.GetStateConnection(hostName, id);

            //В зависимости от состояния блокируем некоторое меню
            if (state == 1 || state == 2)
            {
                //Если на сигнализации или она сработала
                onSignalToolStripMenuItem.Enabled = false;
                notSignalToolStripMenuItem.Enabled = true;

                if (state == 1)
                    stopSignalToolStripMenuItem.Enabled = false;
                else
                    stopSignalToolStripMenuItem.Enabled = true;
            }
            else
            {
                //Если в обычном состоянии
                onSignalToolStripMenuItem.Enabled = true;
                notSignalToolStripMenuItem.Enabled = false;
                stopSignalToolStripMenuItem.Enabled = false;
            }
            onSignalContextToolStripMenuItem.Enabled = onSignalToolStripMenuItem.Enabled;
            noSignalContextToolStripMenuItem.Enabled = notSignalToolStripMenuItem.Enabled;
            stopSignalContextToolStripMenuItem.Enabled = stopSignalToolStripMenuItem.Enabled;
        }

        //Поставить на сигнализацию из контекстного меню
        private void onSignalContextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            onSignalToolStripMenuItem.PerformClick();
        }

        //Снять с сигнализации из контекстного меню
        private void noSignalContextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            notSignalToolStripMenuItem.PerformClick();
        }

        //Отключить сигнал из контекстного меню
        private void stopSignalContextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stopSignalToolStripMenuItem.PerformClick();
        }

        //Открытие контекстного меню
        private void nodeContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TreeNode node = treeView1.SelectedNode;
            if (node == null || node.Parent == null)
                return;
            
            HideMenuItems(node);
            onSignalContextToolStripMenuItem.Enabled = onSignalToolStripMenuItem.Enabled;
            noSignalContextToolStripMenuItem.Enabled = notSignalToolStripMenuItem.Enabled;
            stopSignalContextToolStripMenuItem.Enabled = stopSignalToolStripMenuItem.Enabled;
        }

        //Выбор хоста правой кнопкой мыши
        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
                treeView1.SelectedNode = e.Node;
        }

        //Отключить сигнал
        private void stopSignalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Получим выделенный хост в дереве
            TreeNode node = treeView1.SelectedNode;

            //Если ничего не выделенно, или выделен корневой
            if (node == null || node.Parent == null)
                //На выход
                return;

            //Иначе отправляем команду
            string hostName = node.Text;
            int id = Convert.ToInt32(node.Name);
            server.StopSignalAlarmCmd(hostName, id);
        }

        //Выход
        private void button1_Click(object sender, EventArgs e)
        {
            clToolStripMenuItem.PerformClick();
        }

        //Настройки
        private void button2_Click(object sender, EventArgs e)
        {
            setToolStripMenuItem.PerformClick();
        }
    }
}
