using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using System.Windows.Forms;
using SecurityAlarmLibrary;
using System.Media;

namespace ServerAlarm
{
    /// <summary>
    /// Сервер
    /// </summary>
    public class Server
    {
        private Thread _serverThread;
        private IPEndPoint _serverAddress;
        private TcpListener _serverListener;
        private List<ConnectionInfo> _connections;
        private int _maxConnectCount;

        private SoundPlayer _player;
        private bool _isPlay;

        /// <summary>
        /// Информация о соединении
        /// </summary>
        private class ConnectionInfo : IDisposable
        {
            /// <summary>
            /// Состояния компьютера
            /// </summary>
            public enum StateConputer
            {
                Normal = 0,
                OnSignal = 1,
                SignalActiv = 2
            }

            public Socket Socket;
            public Thread Thread;
            public String HostName;
            public NetworkStream Stream;
            public BinaryWriter Writer;
            public BinaryReader Reader;
            public int id;
            public StateConputer state;
            public bool isPlay;

            /// <summary>
            /// Новое соединение
            /// </summary>
            /// <param name="socket">Сокет</param>
            /// <param name="id">ID</param>
            public ConnectionInfo(Socket socket, int id)
            {
                this.Socket = socket;
                this.Stream = new NetworkStream(this.Socket);
                this.Reader = new BinaryReader(this.Stream);
                this.Writer = new BinaryWriter(this.Stream);
                this.id = id;
                this.isPlay = false;
                state = StateConputer.Normal;
            }

            /// <summary>
            /// Строковое представление
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                if(!string.IsNullOrWhiteSpace(HostName))
                    return String.Format("{0}",HostName);
                return "Определяется имя хоста...";
            }

            /// <summary>
            /// Уничтожение объекта
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            //Освобожение ресурсов
            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (Socket != null)
                    {
                        Socket.Dispose();
                        Socket = null;
                    }
                    if (Stream != null)
                    {
                        Stream.Dispose();
                        Stream = null;
                    }
                    if (Writer != null)
                    {
                        Writer.Dispose();
                        Writer = null;
                    }
                    if (Reader != null)
                    {
                        Reader.Dispose();
                        Reader = null;
                    }
                }
            }

            //Деструктор
            ~ConnectionInfo()
            {
                Dispose(false);
            }
        }

        /// <summary>
        /// Новый сервер
        /// </summary>
        /// <param name="ipEndPoint">Структура: ip адрес и порт</param>
        public Server(IPEndPoint serverAddress)
        {
            _serverAddress = serverAddress;
            _connections = new List<ConnectionInfo>();
            _serverListener = new TcpListener(_serverAddress);
            _player = new SoundPlayer(global::ServerAlarm.Properties.Resources.signalka5);
            _isPlay = false;

            _serverThread = new Thread(AcceptConnections) {  IsBackground = true };
            _serverThread.Start();
        }

        //Принемаем новые соединения (обработку каждого помещаем в отдельный поток)
        private void AcceptConnections()
        {
            _serverListener.Start();
            while (true)
            {
                Socket socket = _serverListener.AcceptSocket();
                _maxConnectCount++;

                ConnectionInfo connection = new ConnectionInfo(socket, _maxConnectCount) 
                { 
                    Thread = new Thread(ProcessConnection) { IsBackground = true }
                };
                connection.Thread.Start(connection);                

                lock (_connections) 
                    _connections.Add(connection);
            }
        }

        //Обработка отдельного соединения
        private void ProcessConnection(object state)
        {
            ConnectionInfo connection = (ConnectionInfo)state;
            try
            {
                bool flag = true;
                do
                {
                    string cmd = connection.Reader.ReadString();

                    if (cmd.Contains(SysMessage.CONNECT))
                    {
                        string[] str = cmd.Split(' ');
                        connection.HostName = str[1];
                    }
                    else
                    {
                        switch (cmd)
                        {
                            //Хост разрывает соединение
                            case SysMessage.CLOSE_CONNECT:
                                flag = false;
                                break;

                            //Сигнализация на удаленном хосте включена успешно
                            case SysMessage.OK_VC_SIGNALING:
                                connection.state = ConnectionInfo.StateConputer.OnSignal;
                                break;

                            //Сигнализация на удаленном хосте не включена по кааким то причинам
                            case SysMessage.NOT_VC_SIGNALING:
                                connection.state = ConnectionInfo.StateConputer.Normal;
                                MessageBox.Show("Не удалось поставить на сигнализацию!",
                                    connection.HostName, MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                                break;

                            //Сигнализация на удаленном хосте выключена успешно
                            case SysMessage.OK_EXIT_SIGNALING:
                                connection.state = ConnectionInfo.StateConputer.Normal;
                                break;

                            //Сигнализация на удаленном хосте выключена не успешно
                            case SysMessage.NOT_EXIT_SIGNALING:
                                connection.state = ConnectionInfo.StateConputer.Normal;
                                MessageBox.Show("Не удалось выключить сигнализацию!",
                                    connection.HostName, MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                                break;

                            //Сработала сигнализация на удаленном хосте
                            case SysMessage.SIGNAL_ALARM:
                                connection.state = ConnectionInfo.StateConputer.SignalActiv;
                                if (!_isPlay)
                                {
                                    if (!connection.isPlay)
                                        _player.PlayLooping();
                                    connection.isPlay = true;
                                }
                                break;
                        }
                    }

                } while (flag && connection.Socket.Connected);
            }
            catch (SocketException exp)
            {
                MessageBox.Show(string.Format("SocketException на хосте: {0} - {1}",connection.HostName, exp.Message));
            }
            catch(Exception exp)
            {
                MessageBox.Show(string.Format("Exception на хосте: {0} - {1}", connection.HostName, exp.Message));
            }
            finally
            {
                connection.Writer.Close();
                connection.Reader.Close();
                connection.Stream.Close();
                connection.Socket.Close();
                lock (_connections) 
                    _connections.Remove(connection);
            }
        }

        //Поиск соединения по id и номеру хоста
        private ConnectionInfo SearchConnection(string hostName, int id)
        {
            lock (_connections)
            {
                foreach (ConnectionInfo c in _connections)
                    if (c.id == id && c.HostName == hostName)
                        return c;
                return null;
            }
        }

        /// <summary>
        /// Отправить команду на постановку хоста на сигнализацию
        /// </summary>
        /// <param name="hostName">Имя хоста</param>
        /// <param name="id">ID</param>
        public int GetStateConnection(string hostName, int id)
        {
            ConnectionInfo connection = SearchConnection(hostName, id);
            //Не нашли на выход
            if (connection == null)
                return 0;

            //Устанавливаем состояние
            switch (connection.state)
            {
                //На сигналке
                case ConnectionInfo.StateConputer.OnSignal:
                    return 1;
                //Сигнализация сработала
                case ConnectionInfo.StateConputer.SignalActiv:
                    return 2;
            }
            //Обычное состояние
            return 0;
        }

        /// <summary>
        /// Отправить команду на постановку хоста на сигнализацию
        /// </summary>
        /// <param name="hostName">Имя хоста</param>
        /// <param name="id">ID</param>
        public void VcSignalingCmd(string hostName, int id)
        {
            ConnectionInfo connection = SearchConnection(hostName, id);
            //Не нашли на выход
            if (connection == null)
                return;

            //Если нашли, отправим соответствующую команду серверу
            connection.Writer.Write(SysMessage.VC_SIGNALING);
        }

        /// <summary>
        /// Отправить команду на остановку сигнала
        /// </summary>
        /// <param name="hostName">Имя хоста</param>
        /// <param name="id">ID</param>
        public void StopSignalAlarmCmd(string hostName, int id)
        {
            ConnectionInfo connection = SearchConnection(hostName, id);
            //Не нашли на выход
            if (connection == null)
                return;

            GetPlayerState(connection);

            connection.state = ConnectionInfo.StateConputer.OnSignal;
            //Если нашли, отправим соответствующую команду серверу
            connection.Writer.Write(SysMessage.STOP_SIGNAL_ALARM);
        }

        //Получить статус проигрования
        private void GetPlayerState(ConnectionInfo connection)
        {
            bool flag = false;
            lock (_connections)
            {
                foreach (ConnectionInfo c in _connections)
                    if (c.id == connection.id)
                    {
                        c.isPlay = false;
                        break;
                    }

                foreach (ConnectionInfo c in _connections)
                    if (c.isPlay)
                        flag = true;
                
            }
            if (!flag)
            {
                _isPlay = false;
                _player.Stop();
            }
        }

        /// <summary>
        /// Отправить команду на снятие хоста с сигнализации
        /// </summary>
        /// <param name="hostName">Имя хоста</param>
        /// <param name="id">ID</param>
        public void ExitSignalingCmd(string hostName, int id)
        {
            ConnectionInfo connection = SearchConnection(hostName, id);
            //Не нашли на выход
            if (connection == null)
                return;

            GetPlayerState(connection);

            connection.state = ConnectionInfo.StateConputer.Normal;
            //Если нашли, отправим соответствующую команду серверу
            connection.Writer.Write(SysMessage.EXIT_SIGNALING);
        }

        /// <summary>
        /// Команда всем хостам, о том что сервер выключается
        /// </summary>
        public void ExitServerCmd()
        {
            lock (_connections)
            {
                foreach (ConnectionInfo c in _connections)
                    c.Writer.Write(SysMessage.EXIT_SERVER);
                _connections.Clear();
            }
        }

        /// <summary>
        /// Получить список подключенных хостов
        /// </summary>
        /// <param name="root">Корень дерева</param>
        /// <param name="menu">Контекстное меню</param>
        public void TreeNodesUpdate(TreeNode root, ContextMenuStrip menu)
        {
            //Получим те узлы которые уже есть в дереве
            List<int> idxsTree = new List<int>();
            foreach (TreeNode n in root.Nodes)
                idxsTree.Add(Convert.ToInt32(n.Name));

            List<int> idxsCon = new List<int>();
            //Теперь добавляем новые узлы
            lock (_connections)
            {
                foreach (ConnectionInfo c in _connections)
                {
                    idxsCon.Add(c.id);
                    //Если уже такой узел есть
                    if (idxsTree.Contains(c.id))
                        //пропускаем
                        continue;

                    TreeNode node = new TreeNode(c.HostName, 1, 1)
                    {
                        Name = c.id.ToString()
                    };
                    node.ContextMenuStrip = menu;
                    root.Nodes.Add(node);
                }

                //Удалим страрые узлы
                for (int i = 0; i < root.Nodes.Count; i++)
                {
                    int id = Convert.ToInt32(root.Nodes[i].Name);
                    if (!idxsCon.Contains(id))
                    {
                        root.Nodes.Remove(root.Nodes[i]);
                        i--;
                    }
                }

                //Теперь у всех узлов нужно обновить состояние
                foreach (ConnectionInfo c in _connections)
                {
                    string key = c.id.ToString();
                    TreeNode node = root.Nodes.Find(key, false)[0];
                    if (node == null)
                        continue;

                    //Учтем текущее состояние компьютера
                    switch (c.state)
                    {
                        //Обычное состояние
                        case ConnectionInfo.StateConputer.Normal:
                            node.ImageIndex = 1;
                            node.SelectedImageIndex = 1;
                            break;

                        //Стоит на сигнализации
                        case ConnectionInfo.StateConputer.OnSignal:
                            node.ImageIndex = 3;
                            node.SelectedImageIndex = 3;
                            break;

                        //Сигнализация сработала
                        case ConnectionInfo.StateConputer.SignalActiv:
                            node.ImageIndex = 2;
                            node.SelectedImageIndex = 2;
                            break;
                    }
                }
            }
        }
        
    }
}
