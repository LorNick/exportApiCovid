using System;
using System.Data;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace exportApiCovid
{
    /// <summary>КЛАСС Экспортируем данные ПЦР тестов</summary>
    public static class ExportPcr
    {
        /// <summary>Колличество загружаемых записей</summary>
        static int countRow = 0;
              
        /// <summary>МЕТОД Запускаем расчет</summary>
        public static void Start()
        {
            Program.logger.Info($"Выполняем до {Config.stepOfProgram} шага.");

            // Шаг COUNT - количество записей
            if (Config.stepOfProgram <= Config.eStepOfProgram.PAKAGE)
            {
                bool _isStepCount = StepCount();
                if (Config.stepOfProgram == Config.eStepOfProgram.COUNT)
                    return;
                if (!_isStepCount)
                {
                    Program.logger.Info("  Дальше не идем, записей то нет!");
                    return;
                }
            }

            // Шаг TOKEN - находим рабочий токен
            if (Config.stepOfProgram > Config.eStepOfProgram.COUNT)
            {
                StepToken().Wait();
                if (Config.stepOfProgram == Config.eStepOfProgram.TOKEN)
                    return;
            }

            // Шаг PAKAGE - отправка данных
            if (Config.stepOfProgram == Config.eStepOfProgram.PAKAGE)
            {                                
                // Пытаемся загрузить все доступные для загрузки ПЦР
                for (int i = 0; i < countRow; i += Config.topRow)
                {
                    // Получаем json и отправляем результат ПЦР первых Config.topRow свободных строк
                    StepPakage().Wait();                    

                    // Помечаем загруженные протоколы ПЦР в поле xInfo
                    UpdateProtokolPcr();                   
                }
                return;
            }

            // Шаг STATUS_COUNT - сколько статусов не проверенно
            if (Config.stepOfProgram == Config.eStepOfProgram.STATUS_COUNT)
            {
                StepStatusCount().Wait();
                return;
            }

            // Шаг STATUS_NEW - получение указанного количество статусов
            if (Config.stepOfProgram == Config.eStepOfProgram.STATUS_NEW)
            {
                StepStatusNew().Wait();
                return;
            }

            // Шаг STATUS_ORDER - получение статусов с указанными кодами
            if (Config.stepOfProgram == Config.eStepOfProgram.STATUS_ORDER)
            {
                StepStatusOrder().Wait();
                return;
            }
        }

        /// <summary>МЕТОД Шаг: COUNT. Находим количество записей, минимальный и максимальные коды протоколов</summary>
        /// <returns>true - если количество записей больше нуля, иначе false</returns>
        static bool StepCount()
        {
            Program.logger.Info("Шаг: COUNT");
            Sql.QueryDataSet(Query.CountDataAvailabilityPcr_Select(), "countProtokol");
            DataRow _row = Sql.dataSet.Tables["countProtokol"].Rows[0];
            countRow = Convert.ToInt32(_row["cou"]);
            if (countRow > 0)
            {
                int _minCodProtokol = Convert.ToInt32(_row["minCod"]);
                int _maxCodProtokol = Convert.ToInt32(_row["maxCod"]);
                Program.logger.Info($@"  Доступно данных ПЦР для загрузки: {countRow}, с кодами с {_minCodProtokol} по {_maxCodProtokol}");
                return true;
            }
            Program.logger.Info($@"  Доступно данных ПЦР для загрузки: 0");
            return false;
        }

        /// <summary>МЕТОД Шаг: TOKEN. Получаем рабочий токен доступа</summary>        
        static async Task StepToken()           
        {
            Program.logger.Info("Шаг: TOKEN");
            string _description = "Получаем рабочий токен доступа";
            string _json = $@"{{ ""depart_number"":""{Config.departNumber}"",
                                ""token"":""{Config.startToken}"" }}";
            string _resposnse = await ProcessRepositories("get-depart-token", _json, _description);
            try
            {
                dynamic _jsonObject = JObject.Parse(_resposnse);
                Config.workToken = _jsonObject.body.token;
                Program.logger.Info("  Рабочий токен: " + Config.workToken);               
            }
            catch(JsonReaderException e)
            {
                Program.ExitError(e);
            }
            catch (Exception e)
            {
                Program.ExitError(e);
            }
        }

        /// <summary>POST запрос</summary> 
        /// <param name="urlAction">Путь к указанному действию (запрос токена, отправка ПЦР, запрос проверок)</param>
        /// <param name="json">Тело запроса</param>
        /// <param name="description">Описание текущего действия</param>
        /// <returns>Возвращаем json ответ</returns>
        static async Task<string> ProcessRepositories(string urlAction, string json, string description)
        {
            var _url = new Uri($"{Config.url}/api/v2/order/{urlAction}");
            var _data = new StringContent(json, Encoding.UTF8, "application/json");
            string _resultContent = "";
            try
            {
                Program.logger.Info("  Запрос: " + description);
                HttpClient _httpClient = new HttpClient();
                HttpResponseMessage _response = await _httpClient.PostAsync(_url, _data);
                _resultContent = await _response.Content.ReadAsStringAsync();
                Program.logger.Info("  Результат запроса: " + _response.StatusCode);

                if (_response.StatusCode != System.Net.HttpStatusCode.OK)
                    throw new Exception("Запрос не прошел.");
            }
            catch (HttpRequestException e)
            {
                Program.ExitError(e);
            }
            catch (Exception e)
            {
                Program.ExitError(e);
            }
            return _resultContent;
        }

        /// <summary>МЕТОД Шаг PAKAGE. Загружаем данные ПЦР</summary>
        static async Task StepPakage()
        {
            Program.logger.Info("Шаг: PAKAGE");
            SqlDataReader _sqlDataReader = Sql.QuerySqlDataReader(Query.Pcr_Select());
            var _jsonPcr = new StringBuilder();          
            while (_sqlDataReader.Read())   // соединяем json тело из строк по 2033 символов
            {
                _jsonPcr.Append(_sqlDataReader.GetString(0));               
            }
            _sqlDataReader.Close();
            string _description = "  Импортируем данные";

            if (Config.visibleJsonPakage)
                Program.logger.Info(_jsonPcr);

            string _json = $@"{{ ""depart_number"":""{Config.departNumber}"",
                                 ""token"":""{Config.workToken}"",
                                 ""json"":""{_jsonPcr.Replace("\"", "\\\"")}""}}";

            string _resposnse = await ProcessRepositories("ext-orders-package", _json, _description);            
        }

        /// <summary>МЕТОД Помечаем загруженные протоколы ПЦР</summary>
        static void UpdateProtokolPcr()
        {
            bool _result = Sql.QueryNo(Query.PcrDataSent_Update());
            Program.logger.Info($"  Пометили {Config.topRow} протоколов ПЦР: " + _result);
            try
            {
                if (!_result)
                    throw new Exception($"Не смогли пометить загруженные протоколы!");
            }
            catch (Exception e)
            {
                Program.ExitError(e);
            }
        }

        /// <summary>МЕТОД Шаг STATUS_COUNT. Сколько статусов не проверенно</summary>
        static async Task StepStatusCount()
        {
            Program.logger.Info("Шаг: STATUS_COUNT");
            string _description = "  Сколько статусов не проверенно";
            string _json = $@"{{ ""depart_number"":""{Config.departNumber}"",
                                 ""token"":""{Config.workToken}""}}";
            string _resposnse = await ProcessRepositories("status-count", _json, _description);
            Program.logger.Info(_resposnse);
        }

        /// <summary>МЕТОД Шаг STATUS_NEW. Получение указанного количество статусов</summary>
        static async Task StepStatusNew()
        {
            Program.logger.Info("Шаг: STATUS_NEW");
            string _description = "  Получение указанного количество статусов";
            string _json = $@"{{ ""depart_number"":""{Config.departNumber}"",
                                 ""token"":""{Config.workToken}"",
                                 ""count"":{Config.statusNewCount}}}";
            string _resposnse = await ProcessRepositories("new-status", _json, _description);
            Program.logger.Info(_resposnse);
        }

        /// <summary>МЕТОД Шаг STATUS_ORDER. Получение статусов с указанными кодами</summary>
        static async Task StepStatusOrder()
        {
            Program.logger.Info("Шаг: STATUS_ORDER");
            string _description = "  Получение статусов с указанными кодами";
            string _json = $@"{{ ""depart_number"":""{Config.departNumber}"",
                                 ""token"":""{Config.workToken}"",
                                 ""orders"":{Config.statusOrders}}}";
            string _resposnse = await ProcessRepositories("status-by-orders", _json, _description);
            Program.logger.Info(_resposnse);
        }
    }
}