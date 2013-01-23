using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;

namespace SecurityAlarmLibrary
{
    /// <summary>
    /// Утилиты
    /// </summary>
    public class Utils
    {
        /// <summary>
        /// Шаблон ip адреса
        /// </summary>
        public const string PATTERN_IP = @"([01]?\d\d?|2[0-4]\d|25[0-5])\." +
                                  @"([01]?\d\d?|2[0-4]\d|25[0-5])\." +
                                  @"([01]?\d\d?|2[0-4]\d|25[0-5])\." +
                                  @"([01]?\d\d?|2[0-4]\d|25[0-5])";

        /// <summary>
        /// Шаблон порта
        /// </summary>
        public const string PATTERN_PORT = "^(([0-9]{1,4})|([1-5][0-9]{4})|(6[0-4][0-9]{3})|(65[0-4][0-9]{2})|(655[0-2][0-9])|(6553[0-5]))$";

        /// <summary>
        /// Проверка на валидность ip адреса и порта
        /// </summary>
        /// <param name="ip">Адрес</param>
        /// <param name="port">Порт</param>
        /// <returns></returns>
        public static IPEndPoint isValidAddress(string ip, string port)
        {
            IPAddress ipAddress = null;
            int portAddress = -1;

            if (!IPAddress.TryParse(ip, out ipAddress))
                return null;

            if (!int.TryParse(port, out portAddress))
                return null;

            return new IPEndPoint(ipAddress, portAddress);
        }

        /// <summary>
        /// Конвертирование Bitmap в Массив байт
        /// </summary>
        /// <param name="img">Изображение</param>
        /// <returns></returns>
        public static byte[] ConvertBitmapToByteArray(Bitmap img)
        {
            List<byte> buffer = new List<byte>();
            using (MemoryStream stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                buffer.AddRange(stream.ToArray());
            }
            return buffer.ToArray();
        }

        /// <summary>
        /// Конвертирование Массива байт в Bitmap
        /// </summary>
        /// <param name="buffer">Массив байт</param>
        /// <returns></returns>
        public static Bitmap ConvertByteArrayToBitmap(byte[] buffer)
        {
            Image image = null;
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                image = Bitmap.FromStream(stream);
            }
            return new Bitmap(image);
        }

        /// <summary>
        /// Можно ли запустить приложение
        /// </summary>
        /// <param name="type">Тип 0 - сервер, 1 - клиент</param>
        /// <returns></returns>
        public static bool CanLoadApplication(int type)
        {
            string procName = null;
            switch (type)
            {
                //Для сервера
                case 0:
                    procName = "ServerAlarm";
                    break;

                //Для клиента
                case 1:
                    procName = "ClientAlarm";
                    break;
            }
            if (procName != null)
            {
                Process[] prs = Process.GetProcesses();
                for (int i = 0; i < prs.Length; i++)
                    if (prs[i].ProcessName == procName)
                        return false;
            }
            return true;
        }
    }
}
