using System;

namespace exportApiCovid
{
    /// <summary>КЛАСС для вывода SQL Запросов</summary>
    /// <remarks>Класс используется только для запросов SQL</remarks>
    public static class Query
    {      
        /// <summary>Дата до которого берем записи (Позавчера)</summary>
        static readonly DateTime DAY2 = DateTime.Today.AddDays(-2);

        /// <summary>Условие выборки данных ПЦР для отправки</summary>
        public static string Pcr_Where()
        {
            string _result = $@"
                p.NumShablon = 1000
                and p.xDelete = 0
                and p.CodApstac > 10                      -- только если есть направление в apaNProtokol или astProtokol
                and p.Cod >= {Config.startCodProtokol}    -- начинаем смотреть с указанного кода
                and p.pDate < '{DAY2:MM.dd.yyyy}'         -- по текущий день (по умолчанию позавчера)
                and p.Protokol like '%11#[01]%'           -- с результатом: отрицательно/положительно
                and dbo.jsonif(p.xInfo,'data_sent') = 0   -- не отправленные данные";
            return _result;
        }

        /// <summary>Выбираем данные ПЦР</summary>
        public static string Pcr_Select()
        {
            string _result = $@"
                use Bazis;

                declare @depart_number as nvarchar(40) = '{Config.departNumber}';

                select top {Config.topRow}
                    cast(d.Cod as nvarchar(20)) as 'order.number'
                    ,@depart_number             as 'order.depart'
                    ,'БУЗОО КОД'                as 'order.laboratoryName'
                    ,'1025500741991 КОД'        as 'order.laboratoryOgrn'
                    ,'БУЗОО КОД'                as 'order.name'
                    ,'1025500741991 КОД'        as 'order.ogrn'
                    ,cast(getdate() as date)    as 'order.orderDate'
                    ,(select 
                        '173'                   as 'code'   -- случайный коды выбрал
                        ,'РНК SARS-CoV-2 (COVID-19), качественное определение' as 'name'
                        ,iif(datediff(d, d.biomaterDate, d.readyDate) < 0 or datediff(d, d.biomaterDate, d.readyDate) > 3, d.readyDate, d.biomaterDate) as 'biomaterDate'
                        ,d.readyDate             as 'readyDate'
                        ,cast(d.rezult as int)   as 'result'
                        for json path)           as 'order.serv'
                    ,dbo.GetFamily(k.FAM)        as 'order.patient.surname'
                    ,k.I                         as 'order.patient.name'
                    ,iif(k.O = 'НЕТ', '', k.O)   as 'order.patient.patronymic'
                    ,k.POL                       as 'order.patient.gender'
                    ,cast(k.DR as date)          as 'order.patient.birthday'
                    ,iif(len(replace(k.TelDom, '-', 0)) = 11, right(replace(k.TelDom, '-', 0), 10), '') as 'order.patient.phone'
                    ,''                          as 'order.patient.email'
                    ,case when k.Doc = 11 then 'Паспорт гражданина РФ' when k.Doc = 2 then 'Свидетельство о рождении' else '' end as 'order.patient.documentType'
                    ,iif(k.Doc in (2, 11) , k.Pasp_Nom, '')      as 'order.patient.documentNumber' 
                    ,iif(k.Doc in (2, 11) , k.Pasp_Ser, '')      as 'order.patient.documentSerNumber' 
                    ,replace(replace(k.SNILS, '-', ''), ' ', '') as 'order.patient.snils'    
                    ,iif(len(k.SN) = 17, left(k.SN, 16), '')     as 'order.patient.oms' 
                    ,k.GorodName    as 'order.patient.address.regAddress.town'
                    ,k.Dom          as 'order.patient.address.regAddress.house'
                    ,k.OblName      as 'order.patient.address.regAddress.region'
                    ,k.Kor          as 'order.patient.address.regAddress.building'
                    ,isnull(k.KRName, '') as 'order.patient.address.regAddress.district'
                    ,k.Kva          as 'order.patient.address.regAddress.appartament'
                    ,k.KulName      as 'order.patient.address.regAddress.streetName'
                    ,k.GorodName    as 'order.patient.address.factAddress.town'
                    ,k.Dom          as 'order.patient.address.factAddress.house'
                    ,k.OblName      as 'order.patient.address.factAddress.region'
                    ,k.Kor          as 'order.patient.address.factAddress.building'
                    ,isnull(k.KRName, '') as 'order.patient.address.factAddress.district'
                    ,k.Kva          as 'order.patient.address.factAddress.appartament'
                    ,k.KulName      as 'order.patient.address.factAddress.streetName'
                from (
                    select p.Cod, p.KL, p.pDate as readyDate
                        ,iif(pol.Cod is not null, dbo.GetPoleD(2, pol.Protokol), dbo.GetPoleD(2, ast.Protokol)) as biomaterDate
                        ,left(dbo.GetPole(11, p.Protokol), 1) as rezult
                    from dbo.kdlProtokol as p
                    left join dbo.apaNProtokol as pol on pol.NumShablon = 9957 and pol.KL = p.KL and pol.Cod = p.CodApstac
                    left join dbo.astProtokol as ast on ast.NumShablon = 9957 and ast.KL = p.KL and ast.Cod = p.CodApstac
                    where {Pcr_Where()}
                ) as d
                join dbo.kbol as k on k.KL = d.KL
                where len(k.SNILS) = 14
                order by d.Cod
                for json path";
            return _result;
        }

        /// <summary>Выбираем данные ПЦР за указанную дату</summary>       
        public static string PcrDataSent_Update()
        {
            string _result = $@"
                use Bazis;

                declare @data_sent as varchar(16) = convert(varchar(16), getdate(), 120);

                update dbo.kdlProtokol
                    set xInfo = iif(isjson(xInfo) = 1, json_modify(xInfo,'$.data_sent', @data_sent), 
                                                       json_modify('{{}}','$.data_sent', @data_sent))
                where Cod in (
                    select top ({Config.topRow}) p.Cod
                    from dbo.kdlProtokol as p    
                    join dbo.kbol as k on k.KL = p.KL and len(k.SNILS) = 14
                    where {Pcr_Where()}
                    order by p.Cod)";
            return _result;
        }

        /// <summary>Количество необработанных протоколов</summary>
        /// <remarks>Выводит количество протоколов, максимальный и минимальный код протокола.
        ///          Если указанно количество циклов (Config.forTest > 0), то берем не более (циклов * записей в ордере) </remarks>
        public static string CountDataAvailabilityPcr_Select()
        {
            string _top = Config.forTest > 0 ? $"top {Config.forTest * Config.topRow}" : "";
            string _result = $@"
                use Bazis;

                select count(d.Cod) as cou, min(d.Cod) as minCod, max(d.Cod) as maxCod
                from (
                    select {_top} p.Cod
                    from dbo.kdlProtokol as p    
                    join dbo.kbol as k on k.KL = p.KL and len(k.SNILS) = 14
                    where {Pcr_Where()}
                ) as d";
            return _result;
        }

        public static string Test_Select()
        {           
            string _result = $@"
                SELECT top(10) [Protokol]                  
              FROM [Bazis].[dbo].[apaNProtokol]
              where Cod = 308769";
            return _result;
        }
    }
}