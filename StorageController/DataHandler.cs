﻿using Microsoft.Data.SqlClient;
using System.Data;

namespace StorageController
{
    public class DataHandler
    {

        private static DataHandler? _instance;
        public static DataHandler instance
        {
            get 
            {
                if (_instance == null)
                {
                    string? DB_IP = Environment.GetEnvironmentVariable("DB_IP");
                    string? DB_PASS = Environment.GetEnvironmentVariable("DB_PASS");

                    if (DB_IP == null || DB_PASS == null)
                        Environment.Exit(1);

                    _instance = new DataHandler(DB_IP, DB_PASS);
                }

                return _instance;
            }
        }

        private string connectionString = 
                "User id=SA;" +
                "TrustServerCertificate=True;"; 

        private DataHandler(string DB_IP, string DB_PASS)
        {

            connectionString += 
                $"Data Source={DB_IP};" +
                $"Password={DB_PASS};";

            CreateDatabase();
            CreateTablesAsync();
        }

        private SqlConnection CreateConnection()
        {

            return new SqlConnection(connectionString);
            
        }

        private async void CreateDatabase()
        {

            // making sure the database exists
            await StaticNonQuery(
                "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'ethereal_storage') BEGIN " +
                "CREATE DATABASE ethereal_storage;" +
                "END;"
            );

            connectionString += "Initial catalog=ethereal_storage;";

        }

        private async void CreateTablesAsync()
        {

            // make sure the schema exists
            await StaticNonQuery(
                "IF NOT EXISTS (SELECT name FROM sys.schemas WHERE name = N'ethereal') BEGIN " +
                "EXEC('CREATE SCHEMA ethereal;');" +
                "END;"
            );

            await StaticNonQuery(
                "IF OBJECT_ID(N'[ethereal].[Users]', N'U') IS NULL " +
                "CREATE TABLE [ethereal].[Users] (" +
                "[UserID] INT IDENTITY (1, 1) NOT NULL, " +
                "[Username] VARCHAR (64) NOT NULL, " +
                "[Email] VARCHAR (64) NOT NULL, " +
                "[Password] VARCHAR (64) NOT NULL, " +
                "[Administrator] BIT NOT NULL, " +
                "PRIMARY KEY CLUSTERED ([UserID] ASC)" +
                ");"
            );

            await StaticNonQuery(
                "IF OBJECT_ID(N'[ethereal].[Files]', N'U') IS NULL " +
                "CREATE TABLE [ethereal].[Files] (" +
                "[FileID] INT IDENTITY (1, 1) NOT NULL," +
                "[Folder] BIT NOT NULL," +
                "[FileType] VARCHAR (20) NULL," +
                "[FileName] VARCHAR (32) NOT NULL," +
                "[FilePassword] VARCHAR (64) NULL," +
                "[FileRoute] INT NULL," +
                "[Location] VARCHAR (8) NULL," +
                "[CreationDate] DATE NULL," +
                "[RedundantID] INT NULL," +
                "PRIMARY KEY CLUSTERED ([FileID] ASC)" +
                ");"
            );

            await StaticNonQuery(
                "IF OBJECT_ID(N'[ethereal].[UserFiles]', N'U') IS NULL " +
                "CREATE TABLE [ethereal].[UserFiles] (" +
                "[FileID] INT NOT NULL," +
                "[UserID] INT NOT NULL," +
                "[Privilege] VARCHAR (10) NOT NULL," +
                "CONSTRAINT [Link_PK] PRIMARY KEY CLUSTERED ([FileID] ASC, [UserID] ASC)," +
                "CONSTRAINT [File_FK] FOREIGN KEY ([FileID]) REFERENCES [ethereal].[Files] ([FileID]) ON DELETE CASCADE," +
                "CONSTRAINT [User_FK] FOREIGN KEY ([UserID]) REFERENCES [ethereal].[Users] ([UserID]) ON DELETE CASCADE" +
                ");"
            );

        }

        /// <summary>
        /// This is used for queries that do not return data and do not have any parameters.
        /// DO NOT INSERT USER GENERATED STRINGS INTO THE SQL TO USE THIS METHOD.
        /// Ie. DO NOT PUT "SELECT * FROM ethereal.Users WHERE UserID = " + userInput;
        /// Doing that would allow SQL injection, use the methods created specificially for queries with parameters.
        /// </summary>
        /// <param name="sql_query">The SQL statement to run.</param>
        /// <returns>How many rows were affected.</returns>
        public async Task<int> StaticNonQuery(string sql_query)
        {

            using ( SqlConnection connection = CreateConnection() )
            {

                OpenSQLConnection(connection);

                SqlCommand command = connection.CreateCommand();
                command.CommandText = sql_query;
                
                int rows_affected = command.ExecuteNonQuery();

                return rows_affected;

            }

        }

        /// <summary>
        /// Use this query if you want to use user input as parameters in the query, this method is used for insert, delete, etc.
        /// This does not return any row data.
        /// </summary>
        /// <param name="sql_query">The SQL command</param>
        /// <param name="parameters">An array of SQL parameters for your query</param>
        /// <returns>The amount of rows affected.</returns>
        public async Task<int> ParametizedNonQuery(string sql_query, SqlParameter[] parameters)
        {

            using (SqlConnection connection = CreateConnection())
            {

                OpenSQLConnection(connection);

                SqlCommand command = connection.CreateCommand();
                command.CommandText = sql_query;
                command.Prepare();
                
                foreach (SqlParameter param in parameters)
                {
                    command.Parameters.Add(param);
                }

                int rows_affected = command.ExecuteNonQuery();

                return rows_affected;

            }

        }

        /// <summary>
        /// This is used for queries that do not have any parameters.
        /// DO NOT INSERT USER GENERATED STRINGS INTO THE SQL TO USE THIS METHOD.
        /// Ie. DO NOT PUT "SELECT * FROM ethereal.Users WHERE UserID = " + userInput;
        /// Doing that would allow SQL injection, use the methods created specificially for queries with parameters.
        /// </summary>
        /// <param name="sql_query">The SQL statement to run.</param>
        /// <returns>The data from your query</returns>
        public async Task<DataTable> StaticQuery(string sql_query)
        {

            using (SqlConnection connection = CreateConnection())
            {

                OpenSQLConnection(connection);

                SqlCommand command = connection.CreateCommand();
                command.CommandText = sql_query;

                SqlDataReader data_reader = command.ExecuteReader();

                DataTable dataTable = new DataTable();
                dataTable.Load(data_reader);

                return dataTable;

            }

        }

        /// <summary>
        /// Use this query if you want to use user input as parameters in the query, this method is used for select, etc.
        /// This returns all the data retrieved according to your query
        /// </summary>
        /// <param name="sql_query">The SQL command</param>
        /// <param name="parameters">An array of SQL parameters for your query</param>
        /// <returns>The data from the query</returns>
        public async Task<DataTable> ParametizedQuery(string sql_query, SqlParameter[] parameters)
        {

            using (SqlConnection connection = CreateConnection())
            {

                OpenSQLConnection(connection);

                SqlCommand command = connection.CreateCommand();
                command.CommandText = sql_query;
                command.Prepare();

                foreach (SqlParameter param in parameters)
                {
                    command.Parameters.Add(param);
                }

                SqlDataReader data_reader = command.ExecuteReader();

                DataTable dataTable = new DataTable();
                dataTable.Load(data_reader);

                return dataTable;

            }

        }

        /// <summary>
        /// Opens the connection, has a timeout of 10 seconds (attemps to connect each second if failed)
        /// </summary>
        /// <param name="connection">The connection to open</param>
        private void OpenSQLConnection(SqlConnection connection)
        {

            int attempts = 10;

            while (attempts >= 0)
            {
                try
                {
                    connection.Open();
                    return;
                }
                catch (Exception exception)
                {
                    Thread.Sleep(1000);

                    // Exiting if there is no attempts left as the controller could not connect to the database
                    if (attempts == 0)
                        throw;

                    attempts--;
                }
            }
        }

        public async Task<Dictionary<string, string[]>?> DataTableToDictionary(DataTable table)
        {

            // Returning null if there's no rows
            if (table.Rows.Count < 1)
                return null;

            // Getting an array of all the rows
            DataRow[] rows = new DataRow[table.Rows.Count];
            table.Rows.CopyTo(rows, 0);

            // Creating the dictionary to return
            Dictionary<string, string[]> columnData = new Dictionary<string, string[]>();

            // Looping over each column and adding its entry to the dictionary + an empty array that can fit all the rows
            foreach(object columnName in table.Columns)
            {
                columnData.Add(columnName.ToString(), new string[table.Rows.Count]);
            }
            
            // Looping over all the rows and adding all their data into the dictionary in the appropriate column and index
            for (int columnIndex = 0; columnIndex < table.Columns.Count; columnIndex++)
            {
                for (int i = 0; i < rows.Length; i++)
                {
                    columnData[table.Columns[columnIndex].ToString()][i] = rows[i][columnIndex].ToString();
                }
            }

            return columnData;

        }
    }
}