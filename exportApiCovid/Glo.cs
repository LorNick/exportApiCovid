using System;
using System.Data;
using NLog;

namespace exportApiCovid
{
    /// <summary>КЛАСС Глобальные переменные</summary>
    public static class Glo
    {
        /// <summary>Логгер</summary>
        public static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>База</summary>
        public static DataSet dataSet = new DataSet();

        /// <summary>Если ошибка то выходим из программы</summary>
        public static bool flagError = false;

        /// <summary>Выходим с ошибкой, выводим её в логи и закрываем приложение</summary> 
        /// <param name="exception">Ошибка Exception</param>
        public static void ExitError(Exception exception)
        {
            logger.Fatal(exception);
            logger.Info(" ------- The End (ошибка)-------\n");

            if (Config.closeConsoleKey)
                Console.ReadKey();

            Environment.Exit(0);
        }
    }
}
