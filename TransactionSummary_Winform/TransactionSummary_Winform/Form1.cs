using MySql.Data.MySqlClient;
using System.Configuration;
using System.Data;
using System.Text;

namespace TransactionSummary_Winform
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.Load += new System.EventHandler(this.Form1_Load); // Hook up the Load event
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            int currentYear = DateTime.Now.Year;

            // Populate the year dropdowns with years from current year to 2-3 years ahead
            for (int year = currentYear; year <= currentYear + 3; year++)
            {
                comboBoxStartYear.Items.Add(year);
                comboBoxEndYear.Items.Add(year);
            }

            // Set default selection
            comboBoxStartYear.SelectedIndex = 0; // current year
            comboBoxEndYear.SelectedIndex = comboBoxEndYear.Items.Count - 1; // last year in range

            // Populate the month dropdowns with values from 1 to 12
            for (int month = 1; month <= 12; month++)
            {
                comboBoxStartMonth.Items.Add(month);
                comboBoxEndMonth.Items.Add(month);
            }

            // Set default selection
            comboBoxStartMonth.SelectedIndex = 0; // January
            comboBoxEndMonth.SelectedIndex = 11; // December
        }
        private void buttonProcess_Click(object sender, EventArgs e)
        {
            try
            {
                string[] transactionTypes = textBoxTransactionTypes.Text.Split(',');
                int startYear = (int)comboBoxStartYear.SelectedItem;
                int endYear = (int)comboBoxEndYear.SelectedItem;
                int startMonth = (int)comboBoxStartMonth.SelectedItem;
                int endMonth = (int)comboBoxEndMonth.SelectedItem;

                foreach (string trxType in transactionTypes)
                {
                    ProcessTransactionType(trxType, startYear, endYear, startMonth, endMonth);
                }

                labelStatus.Text = "Processing completed successfully.";
            }
            catch (Exception ex)
            {
                labelStatus.Text = $"An error occurred: {ex.Message}";
            }
        }

        private void ProcessTransactionType(string trxType, int startYear, int endYear, int startMonth, int endMonth)
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

        private void ProcessMonth(string yearMonthStr, string trxType)
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
            string connectionString = "server=10.98.14.12;Port=3308;User Id=root;database=gsb_logview;password=P@ssw0rd;CharSet=utf8;Allow User Variables=true;";

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
                    string tableName = $"transaction_summary_{yearMonthStr}_{trxType}";

                    // Check if the table exists
                    if (!TableExists(connection, tableName))
                    {
                        // Create the table if it does not exist
                        CreateTransactionSummaryTable(connection, tableName, year, month);
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

                // Update the status on the form (optional)
                labelStatus.Text = $"Data inserted successfully for {yearMonthStr} - {trxType}.";
            }
            catch (Exception ex)
            {
                // Handle any errors
                labelStatus.Text = $"An error occurred for {yearMonthStr} - {trxType}: {ex.Message}";
            }
        }
        private string BuildEjHistoryQuery(DateTime fromDate, DateTime toDate, string terminalQuery, string trxstatusStr)
        {
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
        private string BuildTermProbSlaQuery(DateTime fromDate, DateTime toDate, string terminalQuery)
        {
            StringBuilder queryBuilder = new StringBuilder();

            if (fromDate.ToString("yyyy-MM-dd") == toDate.ToString("yyyy-MM-dd"))
            {
                queryBuilder.AppendLine(@"SELECT fdi.TERM_ID as TerminalNo,fdi.TERM_NAME as TerminalName,fdi.TYPE_ID as TerminalType,fdi.TERM_SEQ as DeviceSerialNo, COALESCE(_" + fromDate.ToString("yyyyMMdd") + "._" + fromDate.ToString("yyyyMMdd") + ",0) as _" + fromDate.ToString("yyyyMMdd") + "");
            }
            else
            {
                queryBuilder.AppendLine(@"SELECT fdi.TERM_ID as TerminalNo,fdi.TERM_NAME as TerminalName,fdi.TYPE_ID as TerminalType,fdi.TERM_SEQ as DeviceSerialNo, COALESCE(_" + fromDate.ToString("yyyyMMdd") + "._" + fromDate.ToString("yyyyMMdd") + ",0) as _" + fromDate.ToString("yyyyMMdd") + ",");
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
        private bool TableExists(MySqlConnection connection, string tableName)
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
        private void CreateTransactionSummaryTable(MySqlConnection connection, string tableName, int year, int month)
        {
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
                string dateColumn = $"_{year}{month:D2}{day:D2}";
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