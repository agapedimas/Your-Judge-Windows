using Your_Judge.Pages;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Windows.Media.Protection.PlayReady;
using Windows.Storage;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace Your_Judge.Classes
{
    static class Data
    {
        private static string DatabasePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "progress.db");
        public async static void Initialize()
        {
            await ApplicationData.Current.LocalFolder.CreateFileAsync("progress.db", CreationCollisionOption.OpenIfExists);
            
            using (var db = new SqliteConnection($"Filename={DatabasePath}"))
            {
                db.Open();

                string command = 
                    "CREATE TABLE IF NOT " +
                    "EXISTS Progress (" +
                        "Id TEXT PRIMARY KEY, " +
                        "FilePath TEXT, " +
                        "FileToken TEXT, " +
                        "Results TEXT NULL" +
                    ")";

                var createTable = new SqliteCommand(command, db);
                createTable.ExecuteReader();

                command =
                    "CREATE TABLE IF NOT " +
                    "EXISTS Challenges (" +
                        "Id TEXT PRIMARY KEY, " +
                        "AuthorId TEXT NOT NULL, " +
                        "Name TEXT NOT NULL, " +
                        "Topic TEXT," +
                        "Snippet TEXT NOT NULL," +
                        "Time TEXT NOT NULL," +
                        "Cases INT NOT NULL" +
                    ")";

                createTable = new SqliteCommand(command, db);
                createTable.ExecuteReader();

                command =
                    "CREATE TABLE IF NOT " +
                    "EXISTS Authors (" +
                        "Id TEXT PRIMARY KEY, " +
                        "Username TEXT UNIQUE, " +
						"Nickname TEXT NOT NULL, " +
                        "URL TEXT NOT NULL " +
                    ")";

                createTable = new SqliteCommand(command, db);
                createTable.ExecuteReader();
            }
        }
        public static List<T> DatabaseGet<T>(string table, string[] columns, string? selectColumn = null, string? selectValue = null)
        {
            if (table == null || columns == null || columns.Length == 0)
                return new();

            List<T> entries = new();
            using (SqliteConnection db = new SqliteConnection($"Filename={DatabasePath}"))
            {
                db.Open();

                string column = "*";

                if (columns != null && columns.Length > 0)
                    column = string.Join(",", columns);

                string select = "";
                var command = new SqliteCommand();

                if (string.IsNullOrEmpty(selectColumn) == false && string.IsNullOrEmpty(selectValue) == false)
                {
                    select = "WHERE " + selectColumn + "=@" + selectColumn; 
                    command.Parameters.AddWithValue("@" + selectColumn, selectValue);
                }

                command.Connection = db;
                command.CommandText = "SELECT " + column + " FROM " + table + " " + select + ";";

                var query = command.ExecuteReader();

                while (query.Read() == true)
                {
                    var entry = new Dictionary<string, string>();

                    for (int i = 0; i < columns.Length; i++)
                        if (query.IsDBNull(i) == false)
                            entry[columns[i]] = query.GetString(i);

                    string json = JsonConvert.SerializeObject(entry);
                    entries.Add(ConvertToType<T>(json));
                }
            }

            return entries;
        }
        public static void DatabaseSet(string table, string keyColumn, string[] columns, string[] values)
        {
            if (table == null || keyColumn == null || columns == null || columns.Length == 0 || values == null || values.Length == 0 || columns.Length != values.Length)
                return;

            using (SqliteConnection db = new SqliteConnection($"Filename={DatabasePath}"))
            {
                SQLitePCL.Batteries.Init();
                db.Open();

                var command = new SqliteCommand();
                string column = "";
                string value = "";
                string excluded = "";

                for (int i = 0; i < columns.Length; i++)
                {
                    column += columns[i];
                    value += "@" + columns[i];
                    excluded += columns[i] + "=excluded." + columns[i];

                    if (i < columns.Length - 1)
                    {
                        column += ", ";
                        value += ", ";
                        excluded += ", ";
                    }

                    if (values[i] == null)
                        command.Parameters.AddWithValue("@" + columns[i], DBNull.Value);
                    else
                        command.Parameters.AddWithValue("@" + columns[i], values[i]);
                }

                command.Connection = db;
                command.CommandText = 
                    "INSERT INTO " + table + " " +
                        "(" + column + ") VALUES (" + value + ") " +
                    "ON CONFLICT(" + keyColumn + ") DO " +
                        "UPDATE SET " + excluded + ";";

                command.ExecuteReader();
            }
        }
        public static Task DatabaseSet(string table, string keyColumn, string[] columns, List<string[]> valuesEachRows)
        {
            if (table == null || keyColumn == null || columns == null || columns.Length == 0 || valuesEachRows == null || valuesEachRows.Count == 0)
                return Task.CompletedTask;

            using (SqliteConnection db = new SqliteConnection($"Filename={DatabasePath}"))
            {
                SQLitePCL.Batteries.Init();
                db.Open();

                using (var transaction = db.BeginTransaction())
                {
                    foreach (var values in valuesEachRows)
                    {
                        if (columns.Length != values.Length)
                            return Task.CompletedTask;

                        var command = new SqliteCommand();
                        string column = "";
                        string value = "";
                        string excluded = "";

                        for (int i = 0; i < columns.Length; i++)
                        {
                            column += columns[i];
                            value += "@" + columns[i];
                            excluded += columns[i] + "=excluded." + columns[i];

                            if (i < columns.Length - 1)
                            {
                                column += ", ";
                                value += ", ";
                                excluded += ", ";
                            }

                            if (values[i] == null)
                                command.Parameters.AddWithValue("@" + columns[i], DBNull.Value);
                            else
                                command.Parameters.AddWithValue("@" + columns[i], values[i]);
                        }

                        command.Connection = db; 
                        command.Transaction = transaction;
                        command.CommandText =
                            "INSERT INTO " + table + " " +
                                "(" + column + ") VALUES (" + value + ") " +
                            "ON CONFLICT(" + keyColumn + ") DO " +
                                "UPDATE SET " + excluded + ";";

                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }

            return Task.CompletedTask;
        }
        public static Task DatabaseRemove(string table, string? selectColumn = null, string? selectValue = null)
        {
            if (table == null)
                return Task.CompletedTask;

            using (SqliteConnection db = new SqliteConnection($"Filename={DatabasePath}"))
            {
                db.Open();

                string select = "";
                var command = new SqliteCommand();

                if (string.IsNullOrEmpty(selectColumn) == false && string.IsNullOrEmpty(selectValue) == false)
                {
                    select = "WHERE " + selectColumn + "=@" + selectColumn;
                    command.Parameters.AddWithValue("@" + selectColumn, selectValue);
                }

                command.Connection = db;
                command.CommandText = "DELETE FROM " + table + " " + select + ";";
                command.ExecuteReader();
            }

            return Task.CompletedTask;
        }

        private static readonly HttpClient HttpClient = new HttpClient();
        public static async Task<HttpResult> HttpGet(string path)
        {
            try
            {
                HttpClient.DefaultRequestHeaders.Accept.Clear();
                HttpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");

                var response = await HttpClient.GetAsync(new Uri("https://" + Variables.Host + path));
                response.EnsureSuccessStatusCode();

                return new() { Status = HttpResult.HttpStatus.Success, Data = await response.Content.ReadAsStringAsync() };
            }
            catch (Exception exception)
            {
                if (exception is COMException)
                {
                    COMException ex = (COMException)exception;
                    Debug.WriteLine(ex.ErrorCode);
                }

                return new() { Status = HttpResult.HttpStatus.Error, Error = "" };
            }
        }
        public static async Task<HttpResult> HttpPost(string path, string jsonBody = "")
        {
            try
            {
                HttpClient.DefaultRequestHeaders.Accept.Clear();
                HttpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");

                var content = new HttpStringContent(jsonBody);
                content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/json");

                var response = await HttpClient.PostAsync(new Uri("https://" + Variables.Host + path), content);
                response.EnsureSuccessStatusCode();

                return new() { Status = HttpResult.HttpStatus.Success, Data = await response.Content.ReadAsStringAsync() };
            }
            catch (Exception exception)
            {
                if (exception is COMException)
                {
                    COMException ex = (COMException)exception;
                    Debug.WriteLine(ex.ErrorCode);
                }

                return new() { Status = HttpResult.HttpStatus.Error, Error = "" };
            }
        }
        public static async Task HttpGetStream(string path, Action<string> onEvent)
        {
            try
            {
                HttpClient.DefaultRequestHeaders.Accept.Clear();
                HttpClient.DefaultRequestHeaders.Accept.TryParseAdd("text/event-stream");

                var response = await HttpClient.GetAsync(new Uri("https://" + Variables.Host + path), HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using var inputStream = await response.Content.ReadAsInputStreamAsync();
                using var stream = inputStream.AsStreamForRead();
                using var reader = new StreamReader(stream);

                string? line;
                string dataBuffer = "";

                while (true)
                {
                    line = await reader.ReadLineAsync();

                    if (line == null)
                        break;

                    if (line.StartsWith("data:"))
                    {
                        dataBuffer += line.Substring(5).Trim();
                    }
                    else if (string.IsNullOrWhiteSpace(line))
                    {
                        if (!string.IsNullOrEmpty(dataBuffer))
                        {
                            onEvent?.Invoke(dataBuffer);
                            dataBuffer = "";
                        }
                    }
                }
            }
            catch
            {
                onEvent?.Invoke("DONE");
            }
        }
        public static List<T> ConvertToList<T>(this string json)
        {
            if (json == null)
                return new();

            List<T>? result = JsonConvert.DeserializeObject<List<T>>(json);

            if (result == null)
                return [];
            else
                return result;
        }
        public static T? ConvertToType<T>(this string json)
        {
            if (json == null)
                return default(T);

            T? result = JsonConvert.DeserializeObject<T>(json);

            if (result == null)
                return default!;
            else
                return result;
        }
        internal class HttpResult
        {
            public enum HttpStatus { Success, Error };
            public HttpStatus Status { get; set; }
            public string? Data { get; set; }
            public string? Error { get; set; }
        }
    }
}
