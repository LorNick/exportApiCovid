using System.Configuration;
using System;

namespace exportApiCovid
{
    /// <summary>КЛАСС Глобальные из конфиг файла App.config (он же в релизе exportApiCovid.exe.Config)</summary>
    public static class Config
    {
        /// <summary>Шаги выполнения программы</summary>
        /// <remarks>В первую очередь для тестов</remarks>
        public enum eStepOfProgram
        {
            /// <summary>Колличество записей</summary>
            COUNT,
            /// <summary>Находим рабочий токен</summary>
            TOKEN,
            /// <summary>Отправка данных</summary>
            PAKAGE,
            /// <summary>Сколько статусов не проверенно</summary>
            STATUS_COUNT,
            /// <summary>Получение указанного количество статусов</summary>
            STATUS_NEW,
            /// <summary>Получение статусов с указанными кодами</summary>
            STATUS_ORDER
        }

        /// <summary>Шаг программы, можно выбрать только один (берем из конфига)</summary>
        public static eStepOfProgram stepOfProgram;

        /// <summary>Код нашего ЛПУ (берем из конфига)</summary>
        public static string departNumber;

        /// <summary>Начальный Токен (берем из конфига)</summary>
        public static string startToken;

        /// <summary>Рабочий Токен (берем из запроса)</summary>
        public static string workToken;

        /// <summary>Путь к серверу (берем из конфига)</summary>
        public static string url;

        /// <summary>Стартовый Cod kdlProtokol (берем из конфига)</summary>
        public static int startCodProtokol;

        /// <summary>Сколько тестовых циклов провести, если 0 то все циклы прогоняем (берем из конфига)</summary>
        public static int forTest;

        /// <summary>Сколько строк отправляем за один раз, максимум 50 (берем из конфига)</summary>
        public static int topRow;

        /// <summary>Количество стасов которые нужно вернуть для Шага STATUS_NEW (берем из конфига)</summary>       
        public static int statusNewCount;

        /// <summary>Коды для получения их статуса, для Шага STATUS_ORDER (берем из конфига)</summary>       
        public static string statusOrders;

        /// <summary>Закрывать консоль по нажатию любой клавиши - true, или закрывать автоматом - false (берем из конфига)</summary>       
        public static bool closeConsoleKey;

        /// <summary>Отображать отправленные данны в логах - true, не показывать - false (только для PAKAGE)</summary>       
        public static bool visibleJsonPakage;

        /// <summary>Читаем данные из конфиг файла </summary>
        /// <remarks>Успех считывания данных из конфиг файла</remarks>
        public static void LoadAppConfig()
        {
            closeConsoleKey = bool.Parse(ReadAppConfig("closeConsoleKey"));

            try
            {
                stepOfProgram = (eStepOfProgram)Enum.Parse(typeof(eStepOfProgram), ReadAppConfig("stepOfProgram"));
            }
            catch (ArgumentException e) // указанно неправильное значение ключа
            {
                Program.ExitError(e);
            }

            if (stepOfProgram > eStepOfProgram.COUNT)
            {
                departNumber = ReadAppConfig("departNumber");
                startToken = ReadAppConfig("token");
                url = ReadAppConfig("url");
            }

            if (stepOfProgram <= eStepOfProgram.PAKAGE)
            {
                startCodProtokol = int.Parse(ReadAppConfig("startCodProtokol"));
                forTest = int.Parse(ReadAppConfig("forTest"));
                topRow = int.Parse(ReadAppConfig("topRow"));
                visibleJsonPakage = bool.Parse(ReadAppConfig("visibleJsonPakage"));
            }
            
            if (stepOfProgram == eStepOfProgram.STATUS_NEW)
                statusNewCount = int.Parse(ReadAppConfig("statusNewCount"));

            if (stepOfProgram == eStepOfProgram.STATUS_ORDER)
                statusOrders = ReadAppConfig("statusOrders");
        }

        /// <summary>Читаем данные из конфиг файле</summary>
        /// <param name="key">Ключ</param>
        /// <param name="isTryEmpty">Наличие ключа в файле, если false - то игнорируем, 
        ///     если true (по умолчанию) - выдаем исключение</param>
        /// <remarks>Считывание данных из раздела appSettings</remarks>
        public static string ReadAppConfig(string key, bool isTryEmpty = true) 
        {
            try
            {
                var _appSettings = ConfigurationManager.AppSettings;

                if (isTryEmpty && _appSettings[key] == null)
                    throw new Exception($"В config файле, отсутствует ключ: {key}");

                var _result = _appSettings[key] ?? string.Empty;
                return _result;
            }
            catch (ConfigurationErrorsException e)
            {
                Program.ExitError(e);
            }
            catch (Exception e)
            {
                Program.ExitError(e);
            }
            return string.Empty;
        }

        /// <summary>Дабавляем/меняем данные в конфиг файле</summary>
        /// <param name="key">Ключ</param>
        /// <param name="value">Значение</param>
        /// <remarks>НЕ используется</remarks>
        public static void AddUpdateAppConfig(string key, string value)
        {
            try
            {
                var _configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var _settings = _configFile.AppSettings.Settings;
                if (_settings.Count == 0 | _settings[key] == null)
                {
                    _settings.Add(key, value);
                }
                else
                {
                    _settings[key].Value = value;
                }
                _configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(_configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException e)
            {
                Program.ExitError(e);
            }
        }
    }
}