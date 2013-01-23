using System;

namespace SecurityAlarmLibrary
{
    /// <summary>
    /// Сообщения для обмена между сервером и клиентом
    /// </summary>
    public class SysMessage
    {
        /// <summary>
        /// Соединение
        /// </summary>
        public const String CONNECT = "CONNECT";

        /// <summary>
        /// Закрытие соединения
        /// </summary>
        public const String CLOSE_CONNECT = "CLOSE";

        /// <summary>
        /// Включить сигнализацию на удаленном хосте
        /// </summary>
        public const String VC_SIGNALING = "VC_SIGNALING";

        /// <summary>
        /// Выключить сигнализацию на удаленном хосте
        /// </summary>
        public const String EXIT_SIGNALING = "EXIT_SIGNALING";

        /// <summary>
        /// Сигнализация успешно включена на удаленном хосте
        /// </summary>
        public const String OK_VC_SIGNALING = "OK_VC_SIGNALING";

        /// <summary>
        /// Сигнализация не включена на удаленном хосте
        /// </summary>
        public const String NOT_VC_SIGNALING = "NOT_VC_SIGNALING";

        /// <summary>
        /// Сигнализация успешно выключена на удаленном хосте
        /// </summary>
        public const String OK_EXIT_SIGNALING = "OK_EXIT_SIGNALING";

        /// <summary>
        /// Сигнализация не выключена на удаленном хосте
        /// </summary>
        public const String NOT_EXIT_SIGNALING = "NOT_EXIT_SIGNALING";

        /// <summary>
        /// Выключение сервера
        /// </summary>
        public const String EXIT_SERVER = "EXIT_SERVER";

        /// <summary>
        /// Сигнал тревоги на сервер
        /// </summary>
        public const String SIGNAL_ALARM = "SIGNAL_ALARM";

        /// <summary>
        /// Отключить текущий сигнал тревоги и продолжить наблюдение
        /// </summary>
        public const String STOP_SIGNAL_ALARM = "STOP_SIGNAL_ALARM";
    }
}
