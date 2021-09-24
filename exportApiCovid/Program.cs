using System.Reflection;
using System;
using NLog;

namespace exportApiCovid
{
    /// <summary>КЛАСС стартуем</summary>
    public static class Program
    {
        /// <summary>Логгер</summary>
        public static Logger logger = LogManager.GetCurrentClassLogger();
                     
        /// <summary>Стартуем</summary>
        static void Main(string[] args)
        {
            string _ver = Assembly.GetEntryAssembly().GetName().Version.ToString().Substring(4);
            logger.Info($" ------- Стартуем (версия {_ver}, 21.09.2021) -------");
            
            Config.LoadAppConfig();
                        
            ExportPcr.Start();

            logger.Info(" ------- The End (успех)-------\n");

            if (Config.closeConsoleKey)
                Console.ReadKey();
        }

        /// <summary>Выходим с ошибкой, выводим ошибку в логи и закрываем приложение</summary> 
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