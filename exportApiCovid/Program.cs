using System.Reflection;
using System;
using NLog;
using System.Net.Mail;
using System.Net;

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

            if (Config.mailIs && DateTime.Today.Day == 1)
            {
                string _message = $@"<p>Автоматическое уведомление о том что программа работает 
                                     <br>Сегодня обновилось {ExportPcr.countRow} случаев (даже если 0, это нормально)
                                     <br>Данное сообщение приходит ежемесячно, каждое {DateTime.Today.Day} число</p>";
                EmailSender("Уведомление", _message);
            }

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

            if (Config.mailIs)
                EmailSender("Ошибка", exception.ToString());

            if (Config.closeConsoleKey)
                Console.ReadKey();

            Environment.Exit(0);
        }

        /// <summary>Отправляем сообщение на почту (ошибки или напоминание что программа жива)</summary> 
        /// <param name="title">Приставка к заголовку</param>
        /// <param name="message">Сообщение</param>
        public static void EmailSender(string title, string message)
        {
            try
            {
                // отправитель - устанавливаем адрес и отображаемое в письме имя
                MailAddress mailFrom = new MailAddress(Config.mailFrom, "exportApiCovid");
                // кому отправляем
                MailAddress mailTo = new MailAddress(Config.mailTo);
                // создаем объект сообщения
                MailMessage mailMessage = new MailMessage(mailFrom, mailTo);
                // тема письма
                mailMessage.Subject = "Программа отправки ПЦР на ГосУслуги: " + title;
                // текст письма
                mailMessage.Body = $@"<h3>{title}:</h3>
                                    <p>{message}</p>
                                    <hr> 
                                    <h3>Напомилка:</h3>
                                    <p>Программа <b>exportApiCovid</b> установлена на машине *.*.0.27 (User, 1234), C:\exportApiCovid
                                    <br>Настройка и управление программы с помощью файла конфигурации <b>exportApiCovid.exe.config</b>
                                    <br>Результаты смотрите в логах <b>nlog.log</b>
                                    <br>Для работы необходима запущенная программа шифрования <b>Континент-АП</b>
                                    <br><a href=""https://github.com/LorNick/exportApiCovid"">Программа на GitHub</a>, там же есть инструкция.
                                    <br>Запускается через планировщик задач, каждый день в 22:36
                                    <br>Обрабатывает позавчерашние данные, если были сбои, то отправит эти данные в следующий раз.
                                    <p>Опять же Вы можете отменить рассылку или сменить почтовый адрес в <b>exportApiCovid.exe.config</b></p>
                                    ";
                // письмо представляет код html
                mailMessage.IsBodyHtml = true;
                // адрес smtp-сервера и порт, с которого будем отправлять письмо
                SmtpClient smtp = new SmtpClient(Config.mailSmtp, Config.mailPort);
                // логин и пароль
                smtp.Credentials = new NetworkCredential(Config.mailFrom, Config.mailFromPassword);                
                smtp.EnableSsl = true;
                smtp.Send(mailMessage);
            }
            catch (Exception e)
            {
                logger.Fatal(e);                
            }
        }
    }
}