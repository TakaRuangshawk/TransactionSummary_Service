using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using System.Data.SqlClient;

namespace TransactionSummary_Manual
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string[] transactionTypes = ConfigurationManager.AppSettings["TransactionTypes"].Split(',');
            // Read year and month ranges from config
            int startYear = int.Parse(ConfigurationManager.AppSettings["StartYear"]);
            int endYear = int.Parse(ConfigurationManager.AppSettings["EndYear"]);
            int startMonth = int.Parse(ConfigurationManager.AppSettings["StartMonth"]);
            int endMonth = int.Parse(ConfigurationManager.AppSettings["EndMonth"]);
            foreach (string trxType in transactionTypes)
            {
                ProcessTransactionType(trxType, startYear, endYear, startMonth, endMonth);
            }
        }

        static void ProcessTransactionType(string trxType, int startYear, int endYear, int startMonth, int endMonth)
        {
            // Loop through each month from the start year and month to the end year and month
            for (int year = startYear; year <= endYear; year++)
            {
                int currentStartMonth = (year == startYear) ? startMonth : 1;
                int currentEndMonth = (year == endYear) ? endMonth : 12;

                for (int month = currentStartMonth; month <= currentEndMonth; month++)
                {
                    string yearMonthStr = $"{year}{month:D2}";
                    ProcessMonth(yearMonthStr, trxType);
                }
            }
        }

        static void ProcessMonth(string yearMonthStr, string trxType)
        {
            // Parse the year and month
            int year = int.Parse(yearMonthStr.Substring(0, 4));
            int month = int.Parse(yearMonthStr.Substring(4, 2));

            // Determine the first and last dates of the month
            DateTime fromDate = new DateTime(year, month, 1);
            DateTime toDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            string fromDateStr = fromDate.ToString("yyyy-MM-dd");
            string toDateStr = toDate.ToString("yyyy-MM-dd");

            // Read other configuration values
            string terminalStr = ConfigurationManager.AppSettings["TerminalId"];
            string terminalTypeStr = ConfigurationManager.AppSettings["TerminalType"];
            string connectionString = ConfigurationManager.AppSettings["ConnectString_MySQL"];

            StringBuilder queryBuilder = new StringBuilder();
            string terminalQuery = string.Empty;
            string trxstatusStr = string.Empty;
            string tablequery = string.Empty;
            List<Dictionary<string, object>> resultList = new List<Dictionary<string, object>>();

            // Build terminal query
            if (!string.IsNullOrEmpty(terminalStr))
            {
                terminalQuery += $" AND terminalid = '{terminalStr}'";
            }
            if (!string.IsNullOrEmpty(terminalTypeStr))
            {
                terminalQuery += $" AND terminalid LIKE '%{terminalTypeStr}'";
            }

            // Determine transaction type and set query parameters
            switch (trxType)
            {
                case "deposit":
                    terminalQuery += " AND trx_type IN ('DEP_DCA', 'DEP_DCC', 'DEP_P00', 'DEP_P01','RFT_DCA') ";
                    tablequery = "ejhistory";
                    trxstatusStr = " AND trx_status = 'OK' ";
                    break;
                case "withdraw":
                    terminalQuery += " AND trx_type IN ('FAS', 'MCASH', 'WDL','CL_WDL') ";
                    tablequery = "ejhistory";
                    trxstatusStr = " AND trx_status = 'OK' ";
                    break;
                case "receipt":
                    terminalQuery += $" AND a.probcode IN ('SLA_N_1707_04') ";
                    tablequery = "termprobsla";
                    trxstatusStr = "";
                    break;
                case "barcode":
                    terminalQuery += " AND trx_type IN ('BAR_P00','BAR_PCB') ";
                    tablequery = "ejhistory";
                    trxstatusStr = "";
                    break;
                default:
                    terminalQuery += " AND trx_type IN ('DEP_DCA', 'DEP_DCC', 'DEP_P00', 'DEP_P01','RFT_DCA','FAS', 'MCASH', 'WDL','CL_WDL') ";
                    tablequery = "ejhistory";
                    trxstatusStr = " AND trx_status = 'OK' ";
                    break;
            }

            // Build the appropriate SQL query
            if (tablequery == "ejhistory")
            {
                queryBuilder.AppendLine(BuildEjHistoryQuery(fromDate, toDate, terminalQuery, trxstatusStr));
            }
            else if (tablequery == "termprobsla")
            {
                queryBuilder.AppendLine(BuildTermProbSlaQuery(fromDate, toDate, terminalQuery));
            }

            string finalQuery = queryBuilder.ToString();

            try
            {
                // Execute the query and retrieve data
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    // Define the table name dynamically based on current year and month
                    string tableName = $"transaction_summary_{trxType}_{yearMonthStr}";

                    // Check if the table exists
                    if (!TableExists(connection, tableName))
                    {
                        // Create the table if it does not exist
                        CreateTransactionSummaryTable(connection, tableName);
                    }
                    using (MySqlCommand command = new MySqlCommand(finalQuery, connection))
                    {
                        command.CommandType = CommandType.Text;
                        command.CommandTimeout = 180;

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Dictionary<string, object> row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row[reader.GetName(i)] = reader.GetValue(i);
                                }
                                resultList.Add(row);
                            }
                        }
                    }

                    // Insert the data into the target table
                    foreach (var row in resultList)
                    {
                        StringBuilder insertQuery = new StringBuilder($"INSERT INTO {tableName} (");

                        // Append column names
                        foreach (var key in row.Keys)
                        {
                            insertQuery.Append(key + ",");
                        }

                        insertQuery.Length--; // Remove the last comma
                        insertQuery.Append(") VALUES (");

                        // Append values
                        foreach (var value in row.Values)
                        {
                            insertQuery.Append($"'{value}',");
                        }

                        insertQuery.Length--; // Remove the last comma
                        insertQuery.Append($") ON DUPLICATE KEY UPDATE ");

                        // Append update statements, excluding 'No'
                        foreach (var key in row.Keys)
                        {
                            insertQuery.Append($"{key}=VALUES({key}),");
                        }

                        insertQuery.Length--; // Remove the last comma
                        insertQuery.Append(";");

                        using (MySqlCommand insertCommand = new MySqlCommand(insertQuery.ToString(), connection))
                        {
                            insertCommand.CommandType = CommandType.Text;
                            insertCommand.ExecuteNonQuery();
                        }
                    }

                }

                Console.WriteLine("Data inserted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        static string BuildEjHistoryQuery(DateTime fromDate, DateTime toDate, string terminalQuery, string trxstatusStr)
        {
            // Build SQL query for ejhistory
            StringBuilder queryBuilder = new StringBuilder();
            if (fromDate.ToString("yyyy-MM-dd") == toDate.ToString("yyyy-MM-dd"))
            {
                queryBuilder.AppendLine($@"SELECT 
                                           fdi.TERM_ID as TerminalNo,
                                           fdi.TERM_NAME as TerminalName,
                                           fdi.TYPE_ID as TerminalType,
                                           fdi.TERM_SEQ as DeviceSerialNo,
                                           COALESCE(_{fromDate:yyyyMMdd}._{fromDate:yyyyMMdd},0) as _{fromDate:yyyyMMdd}");
            }
            else
            {
                queryBuilder.AppendLine($@"SELECT 
                                           fdi.TERM_ID as TerminalNo,
                                           fdi.TERM_NAME as TerminalName,
                                           fdi.TYPE_ID as TerminalType,
                                           fdi.TERM_SEQ as DeviceSerialNo,
                                           COALESCE(_{fromDate:yyyyMMdd}._{fromDate:yyyyMMdd},0) as _{fromDate:yyyyMMdd},");
            }

            // Add dynamic columns for each day in the range
            for (DateTime date = fromDate.AddDays(1); date.Date <= toDate.Date; date = date.AddDays(1))
            {
                string dateStr = date.ToString("yyyyMMdd");
                if (date != toDate)
                {
                    queryBuilder.AppendLine($"\tCOALESCE(_{dateStr}._{dateStr}, 0) AS _{dateStr},");
                }
                else
                {
                    queryBuilder.AppendLine($"\tCOALESCE(_{dateStr}._{dateStr}, 0) AS _{dateStr}");
                }
            }

            // Join logic
            queryBuilder.AppendLine(@"FROM fv_device_info fdi
                                      LEFT JOIN
                                      (SELECT terminalid,
                                              SUM(CASE WHEN DATE(trx_datetime) = '" + fromDate.ToString("yyyy-MM-dd") + "' THEN 1 ELSE 0 END) AS _" + fromDate.ToString("yyyyMMdd") + @"
                                       FROM ejlog_history as a
                                       WHERE trxid IS NOT NULL " + terminalQuery + @"
                                         AND trx_datetime BETWEEN '" + fromDate.ToString("yyyy-MM-dd") + @" 00:00:00' AND '" + fromDate.ToString("yyyy-MM-dd") + @" 23:59:59'
                                         " + trxstatusStr + @"
                                       GROUP BY terminalid) AS _" + fromDate.ToString("yyyyMMdd") + @" ON fdi.TERM_ID = _" + fromDate.ToString("yyyyMMdd") + ".terminalid");

            // Additional joins for each subsequent day in the range
            for (DateTime date = fromDate.AddDays(1); date.Date <= toDate.Date; date = date.AddDays(1))
            {
                string dateStr = date.ToString("yyyyMMdd");
                queryBuilder.AppendLine($@"LEFT JOIN
                                           (SELECT terminalid,
                                                   SUM(CASE WHEN DATE(trx_datetime) = '" + date.ToString("yyyy-MM-dd") + "' THEN 1 ELSE 0 END) AS _" + dateStr + @"
                                            FROM ejlog_history as a
                                            WHERE trxid IS NOT NULL " + terminalQuery + @"
                                              AND trx_datetime BETWEEN '" + date.ToString("yyyy-MM-dd") + @" 00:00:00' AND '" + date.ToString("yyyy-MM-dd") + @" 23:59:59'
                                              " + trxstatusStr + @"
                                            GROUP BY terminalid) AS _" + dateStr + @" ON fdi.TERM_ID = _" + dateStr + ".terminalid");
            }


            return queryBuilder.ToString();
        }

        static string BuildTermProbSlaQuery(DateTime fromDate, DateTime toDate, string terminalQuery)
        {
            // Build SQL query for termprobsla
            StringBuilder queryBuilder = new StringBuilder();

            if (fromDate.ToString("yyyy-MM-dd") == toDate.ToString("yyyy-MM-dd"))
            {
                queryBuilder.AppendLine(@"  SELECT fdi.TERM_ID as TerminalNo,fdi.TERM_NAME as TerminalName,fdi.TYPE_ID as TerminalType,fdi.TERM_SEQ as DeviceSerialNo, COALESCE(_" + fromDate.ToString("yyyyMMdd") + "._" + fromDate.ToString("yyyyMMdd") + ",0) as _" + fromDate.ToString("yyyyMMdd") + "");
            }
            else
            {
                queryBuilder.AppendLine(@"  SELECT fdi.TERM_ID as TerminalNo,fdi.TERM_NAME as TerminalName,fdi.TYPE_ID as TerminalType,fdi.TERM_SEQ as DeviceSerialNo, COALESCE(_" + fromDate.ToString("yyyyMMdd") + "._" + fromDate.ToString("yyyyMMdd") + ",0) as _" + fromDate.ToString("yyyyMMdd") + ",");
            }

            for (DateTime date = fromDate.AddDays(1); date.Date <= toDate.Date; date = date.AddDays(1))
            {
                string dateStr = date.ToString("yyyyMMdd");
                if (date.ToString("yyyy-MM-dd") != toDate.ToString("yyyy-MM-dd"))
                {
                    queryBuilder.AppendLine("\tCOALESCE(_" + dateStr + "._" + dateStr + ", 0) AS _" + dateStr + ",");
                }
                else
                {
                    queryBuilder.AppendLine("\tCOALESCE(_" + dateStr + "._" + dateStr + ", 0) AS _" + dateStr);
                }
            }

            queryBuilder.AppendLine(@" FROM fv_device_info fdi
                    LEFT JOIN
                    (SELECT 
                        terminalid,
                        SUM(CASE WHEN DATE(trxdatetime) = '" + fromDate.ToString("yyyy-MM-dd") + "' THEN 1 ELSE 0 END) AS _" + fromDate.ToString("yyyyMMdd") + @"
                        FROM 
                            ejlog_devicetermprob_sla as a 
                        WHERE 
                            a.seqno IS NOT NULL " + terminalQuery + @"
                            AND trxdatetime BETWEEN '" + fromDate.ToString("yyyy-MM-dd") + @" 00:00:00' AND '" + fromDate.ToString("yyyy-MM-dd") + @" 23:59:59'
                        GROUP BY 
                            terminalid) AS _" + fromDate.ToString("yyyyMMdd") + " ON fdi.TERM_ID = _" + fromDate.ToString("yyyyMMdd") + ".terminalid");

            for (DateTime date = fromDate.AddDays(1); date.Date <= toDate.Date; date = date.AddDays(1))
            {
                string dateStr = date.ToString("yyyyMMdd");
                queryBuilder.AppendLine(@"    LEFT JOIN
                                (SELECT 
                                    terminalid,
                                    SUM(CASE WHEN DATE(trxdatetime) = '" + date.ToString("yyyy-MM-dd") + "' THEN 1 ELSE 0 END) AS _" + dateStr + @"
                                FROM 
                                    ejlog_devicetermprob_sla as a  
                                WHERE 
                                    a.seqno IS NOT NULL " + terminalQuery + @"
                                    AND trxdatetime BETWEEN '" + date.ToString("yyyy-MM-dd") + @" 00:00:00' AND '" + date.ToString("yyyy-MM-dd") + @" 23:59:59'

                                GROUP BY 
                                    terminalid) AS _" + dateStr + @"
                                ON 
                                    fdi.TERM_ID = _" + dateStr + @".terminalid");
            }

            return queryBuilder.ToString();
        }
        static bool TableExists(MySqlConnection connection, string tableName)
        {
            string query = $"SHOW TABLES LIKE '{tableName}'";
            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    return reader.HasRows;
                }
            }
        }

        static void CreateTransactionSummaryTable(MySqlConnection connection, string tableName)
        {
            // Extract year and month from the table name
            string yearMonth = tableName.Split('_')[2];
            int year = int.Parse(yearMonth.Substring(0, 4));
            int month = int.Parse(yearMonth.Substring(4, 2));

            // Get the number of days in the specified month
            int daysInMonth = DateTime.DaysInMonth(year, month);

            StringBuilder createTableQuery = new StringBuilder($@"
                CREATE TABLE {tableName} (
                    TerminalNo VARCHAR(255),
                    TerminalName VARCHAR(255),
                    TerminalType VARCHAR(255),
                    DeviceSerialNo VARCHAR(255),
            ");

            // Add columns for each day in the month
            for (int day = 1; day <= daysInMonth; day++)
            {
                string dateColumn = $"_{yearMonth}{day:D2}";
                createTableQuery.AppendLine($"{dateColumn} INT DEFAULT 0,");
            }

            // Define the composite primary key to avoid duplication
            createTableQuery.AppendLine($@"
            UNIQUE KEY unique_transaction (TerminalNo, DeviceSerialNo)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            ");

            using (MySqlCommand cmd = new MySqlCommand(createTableQuery.ToString(), connection))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }
}
