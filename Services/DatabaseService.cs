using System;
using DotNetEnv;
using MySql.Data.MySqlClient;

namespace LearnLanguages.Services
{
    public static class DatabaseService
    {
        public static MySqlConnection GetConnection()
        {
            // Ładowanie pliku .env umieszczonego w głównym folderze
            Env.Load();

            string server = Environment.GetEnvironmentVariable("DB_SERVER") ?? "localhost";
            string port = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
            string database = Environment.GetEnvironmentVariable("DB_NAME");
            string user = Environment.GetEnvironmentVariable("DB_USER");
            string password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";

            string connectionString = $"Server={server};Port={port};Database={database};Uid={user};Pwd={password};";

            return new MySqlConnection(connectionString);
        }
    }
}