using System.Reflection;
using System;

namespace exportApiCovid
{
    /// <summary>КЛАСС стартуем</summary>
    public static class Program
    {
        /// <summary>Стартуем</summary>
        static void Main(string[] args)
        {
            string _ver = Assembly.GetEntryAssembly().GetName().Version.ToString().Substring(4);
            Glo.logger.Info($" ------- Стартуем (версия {_ver}, 21.09.2021) -------");
            
            Config.LoadAppConfig();
                        
            ExportPcr.Start();

            Glo.logger.Info(" ------- The End (успех)-------\n");

            if (Config.closeConsoleKey)
                Console.ReadKey();
        }
    }
}
