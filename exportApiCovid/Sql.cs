using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace exportApiCovid
{
    /// <summary>КЛАСС работы с Sql</summary>
    public static class Sql
    {
        /// <summary>Строка подключения к SQL Server`у</summary>
        static readonly SqlConnection sqlConnection;

        // КОНСТРУКТОР
        static Sql()
        {
            sqlConnection = new SqlConnection();
            sqlConnection.ConnectionString = ConnectionSql("exportApiCovid.ConnectionString");
        }

        /// <summary>Находим строку подключения к SQL Server из config файла</summary>
        /// <param name="providerName">Имя строки подключение в config файле</param>
        /// <returns>Возвращаем строку подключения к серверу MS SQL</returns>
        public static string ConnectionSql(string providerName)
        {
            string _connectionString = null;
            ConnectionStringSettings _settings = ConfigurationManager.ConnectionStrings[providerName];
            if (_settings != null)
                _connectionString = _settings.ConnectionString;
            else
                Glo.logger.Fatal("Не нахожу в config файле строку подключения по имени" + providerName);           
            return _connectionString;
        }

        /// <summary>Заполняем DataSet Базиса (<see cref="Glo.dataSet"/>)</summary>
        /// <param name="querySql">Строка SQL кода</param>
        /// <param name="nameTable">Наименование таблицы</param>
        /// <returns>Возвращаем количество загруженных строк</returns>
        public static int QueryDataSet(string querySql, string nameTable)
        {
            int _countTimeout = 0; // попытки достучаться до сервера (5 попыток)
            SqlDataAdapter _sqlData = new SqlDataAdapter(querySql, sqlConnection);
        label1:
            try
            {
                // Предварительно удаляем таблицу
                if (Glo.dataSet.Tables[nameTable] != null)
                    Glo.dataSet.Tables[nameTable].Clear();
                // Загружаем данные
                return _sqlData.Fill(Glo.dataSet, nameTable);
            }
            catch (SqlException ex)
            {
                if (ex.Number == -2 && _countTimeout++ < 5)
                    goto label1;
                ex.Data["SQL"] = querySql;
                Glo.logger.Fatal(ex, "Ошибка Загрузки данных в DataSet из SQL");                
                goto label1;
            }
            catch (Exception ex)
            {                
                Glo.dataSet.Tables.Remove(nameTable);
                Glo.logger.Fatal(ex, "Ошибка Загрузки данных в DataSet из SQL");
                return _sqlData.Fill(Glo.dataSet, nameTable);
            }
        }

        /// <summary>Выполняем запрос (возвращаем строку)</summary>
        /// <param name="querySql">Строка SQL запроса</param>
        /// <returns>Возвращаем строку</returns>
        public static string QueryString(string querySql)
        {
            string _result = "";
            int _countTimeout = 0; // попытки достучаться до сервера (5 попыток)
        label1:
            try
            {
                if (sqlConnection.State == ConnectionState.Closed) sqlConnection.Open();
                SqlCommand _sqlCommand = new SqlCommand(querySql, sqlConnection);
                try
                {
                    _result = (string)_sqlCommand.ExecuteScalar();
                }
                catch (SqlException ex)
                {
                    if (ex.Number == -2 && _countTimeout++ < 5)
                        goto label1;
                    ex.Data["SQL"] = querySql;
                    Glo.logger.Fatal(ex, "Ошибка Запроса SQL (возвращающего строку)");
                }
                catch (Exception ex)
                {
                    ex.Data["SQL"] = querySql;
                    Glo.logger.Fatal(ex, "Ошибка Запроса SQL (возвращающего строку)");
                }
                sqlConnection.Close();
            }
            catch (Exception ex)
            {
                ex.Data["SQL"] = querySql;
                Glo.logger.Fatal(ex, "Ошибка Запроса SQL (возвращающего строку)");               
                goto label1;
            }
            return _result;
        }

        /// <summary>Выполняем запрос (возвращаем целое число)</summary>
        /// <param name="querySql">Строка SQL кода</param>
        /// <returns>Возвращаем целое число</returns>
        public static int QueryInt(string querySql)
        {
            int _result = 0;
            int _countTimeout = 0; // попытки достучаться до сервера (5 попыток)
        label1:
            try
            {
                if (sqlConnection.State == ConnectionState.Closed) sqlConnection.Open();
                SqlCommand _sqlCommand = new SqlCommand(querySql, sqlConnection);
                try
                {
                    int.TryParse(_sqlCommand.ExecuteScalar().ToString(), out _result);                   
                }
                catch (SqlException ex)
                {
                    if (ex.Number == -2 && _countTimeout++ < 5)
                        goto label1;
                    ex.Data["SQL"] = querySql;
                    Glo.logger.Error(ex, "Ошибка Запроса SQL (возвращающего целое число)");
                }
                catch (Exception ex)
                {
                    ex.Data["SQL"] = querySql;
                    Glo.logger.Error(ex, "Ошибка Запроса SQL (возвращающего целое число)");
                }
            }
            catch (Exception ex)
            {
                ex.Data["SQL"] = querySql;
                Glo.logger.Fatal(ex, "Ошибка Запроса SQL (возвращающего целое число)");               
                goto label1;
            }
            sqlConnection.Close();
            return _result;
        }

        /// <summary>Выполняем запрос (без возврата значений)</summary>
        /// <param name="querySql">Строка SQL кода</param>
        /// <returns>true -  успешное выполнение запроса, false - что то пошло не так</returns>
        public static bool QueryNo(string querySql)
        {
            int _countTimeout = 0; // попытки достучаться до сервера (5 попыток)
            bool _result = false;
        label1:
            try
            {
                if (sqlConnection.State == ConnectionState.Closed) sqlConnection.Open();
                SqlCommand _sqlCommand = new SqlCommand(querySql, sqlConnection);
                try
                {
                    _sqlCommand.ExecuteNonQuery();
                    _result = true;
                }
                catch (SqlException ex)
                {
                    if (ex.Number == -2 && _countTimeout++ < 5)
                        goto label1;
                    ex.Data["SQL"] = querySql;
                    Glo.logger.Fatal(ex, "Ошибка Запроса SQL (без возврата значения)");
                }
                catch (Exception ex)
                {
                    ex.Data["SQL"] = querySql;
                    Glo.logger.Fatal(ex, "Ошибка Запроса SQL (без возврата значения)");
                }
            }
            catch (Exception ex)
            {
                ex.Data["SQL"] = querySql;
                Glo.logger.Fatal(ex, "Ошибка Запроса SQL (без возврата значения)");                               
            }
            sqlConnection.Close();
            return _result;
        }

        /// <summary>Выполняем запрос (возвращаем SqlDataReader)</summary>
        /// <param name="querySql">Строка SQL кода</param>
        /// <returns>Возвращаем SqlDataReader</returns>
        public static SqlDataReader QuerySqlDataReader(string querySql)
        {
            int _countTimeout = 0; // попытки достучаться до сервера (5 попыток)
        label1:
            try
            {
                if (sqlConnection.State == ConnectionState.Closed) sqlConnection.Open();
                SqlCommand _sqlCommand = new SqlCommand(querySql, sqlConnection);
                try
                {
                    SqlDataReader _sqlDataReader = _sqlCommand.ExecuteReader(CommandBehavior.CloseConnection);
                    return _sqlDataReader;
                }
                catch (SqlException ex)
                {
                    if (ex.Number == -2 && _countTimeout++ < 5)
                        goto label1;
                    ex.Data["SQL"] = querySql;
                    Glo.ExitError(ex);
                }
                catch (Exception ex)
                {
                    ex.Data["SQL"] = querySql;
                    Glo.ExitError(ex);
                }
            }
            catch (Exception ex)
            {
                ex.Data["SQL"] = querySql;                
                Glo.ExitError(ex);
            }
            return null;
        }    
    }  
}