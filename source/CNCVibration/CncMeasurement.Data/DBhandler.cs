using CncMeasurement.Core.Interfaces;
using CncMeasurement.Core.models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json; // Swapped to Newtonsoft.Json
using System;
using System.Collections.Generic;

using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using CncMeasurement.Core;

namespace CncMeasurement.Data
{
    internal class MeasurementContext : DbContext
    {
        internal DbSet<ExperimentSchema> Experiments { get; set; }

        public string DbPath { get; }

        public MeasurementContext(string dbPath)
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = Path.Join(path, "Measurements.db");
        }

        
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite($"Data Source={DbPath}");
        }
    }

    public class DatabaseController : IDatabaseController
    {
        private FileWritingController FileController;
        ExperimentSetup _currentExperiment;
        MeasurementContext _context;
        private readonly string DBpath;
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            DateParseHandling = DateParseHandling.DateTimeOffset
        };
        public DatabaseController(string path)
        {
            DBpath = path;
            EnsureDatabase();
        }
        public void InitializeContext()
        {
            _context = new MeasurementContext(DBpath);
        }
        public async Task StartLogLiveExperiment(ExperimentSetup setup, ChannelReader<RmsFrame> RMSreader, ChannelReader<FftFrame> FFTreader)
        {
            _currentExperiment = setup;
            FileController = new FileWritingController(_currentExperiment);
            var RMSTask = FileController.WriteCompleteRMSAsync(RMSreader);
            var FFTTask = FileController.WriteCompleteFFTAsync(FFTreader);

            await RMSTask;
            await FFTTask;
        }
        // Returns a JSON string containing an array of summaries (Id, Name, Description, MachineConfig)
        public string ListModalExperimentSchemaSummariesJson()
        {
            Console.WriteLine("[Database] Fetching modal experiment schema summaries from database...");
            var summaries = new List<ModalExperimentSchemaSummary>();

            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, Json FROM ModalExperimentSchemas;";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var idStr = reader.IsDBNull(0) ? null : reader.GetString(0);
                            var json = reader.IsDBNull(1) ? null : reader.GetString(1);

                            if (string.IsNullOrWhiteSpace(idStr) || string.IsNullOrWhiteSpace(json))
                            {
                                Console.WriteLine($"[Database] Skipping entry with missing ID or JSON: ID='{idStr}'");
                                continue;
                            }
                            try
                            {
                                var schema = JsonConvert.DeserializeObject<ModalExperimentSchema>(json, JsonSettings);
                                if (schema == null) continue;

                                summaries.Add(new ModalExperimentSchemaSummary
                                {
                                    ID = idStr,
                                    Name = schema.Name,
                                    Description = schema.Description,
                                    MachineConfig = schema.MachineConfig
                                });
                            }
                            catch (JsonException)
                            {
                                Console.WriteLine($"[Database] Skipping invalid JSON for ID: {idStr}");
                                continue;
                            }
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject(summaries, JsonSettings);
        }

        
        public async Task<List<ModalExperimentSchemaSummary>> ListModalExperimentSchemaSummariesAsync()
        {
            var summaries = new List<ModalExperimentSchemaSummary>();
            Console.WriteLine("[Database] Fetching modal experiment schema summaries from database...");
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, Json FROM ModalExperimentSchemas;";
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var idStr = await reader.IsDBNullAsync(0) ? null : reader.GetString(0);
                            var json = await reader.IsDBNullAsync(1) ? null : reader.GetString(1);

                            if (string.IsNullOrWhiteSpace(idStr) || string.IsNullOrWhiteSpace(json))
                            {
                                Console.WriteLine($"[Database] Skipping entry with missing ID or JSON: ID='{idStr}'");
                                continue;
                            }
                            try
                            {
                                var schema = JsonConvert.DeserializeObject<ModalExperimentSchema>(json, JsonSettings);
                                if (schema == null) continue;

                                summaries.Add(new ModalExperimentSchemaSummary
                                {
                                    ID = idStr,
                                    Name = schema.Name,
                                    Description = schema.Description,
                                    MachineConfig = schema.MachineConfig
                                });
                            }
                            catch (JsonException)
                            {
                                Console.WriteLine($"[Database] Skipping invalid JSON for ID: {idStr}");
                                continue;
                            }
                        }
                    }
                }
            }

            return summaries;
        }
        private async Task SaveCurrentExperiment()
        {
            _context.Add(_currentExperiment);
            await _context.SaveChangesAsync();
        }

        public async Task StopLog()
        {
            await FileController.StopSaving();


            // Create database entries
            await SaveCurrentExperiment();

        }
        // make Csharp types into strings so they can be stored in the database
        private string MapCsharpTypeToSqlite(Type type)
        {
            // Handle nullable types (e.g., int?, double?)
            Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (underlyingType == typeof(int) || underlyingType == typeof(long) || underlyingType == typeof(short) || underlyingType == typeof(bool))
                return "INTEGER"; // SQLite stores booleans as 0 or 1

            if (underlyingType == typeof(double) || underlyingType == typeof(float) || underlyingType == typeof(decimal))
                return "REAL";

            // strings, DateTimes, Guids, etc., are usually stored as TEXT in SQLite
            return "TEXT";
        }

        

        public DBinfo listCollections()
        {
            //code to acces collections

            return null;
        }

        public void ClearDatabase()
        {
            throw new NotImplementedException();
        }


       

        public MeasurementMetadata GetMeasurementByID(string ID)
        {
            return null;
        }

        static string DbPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Measurements.db");

        static SqliteConnection GetConnection()
        {
            var cs = $"Data Source={DbPath}";
            var conn = new SqliteConnection(cs);
            return conn;
        }

        public DatabaseController()
        {
            EnsureDatabase();
        }

        void EnsureDatabase()
        {
            var dir = Path.GetDirectoryName(DbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {

                Directory.CreateDirectory(dir);
            }

            // Opening a SqliteConnection will create the database file if it does not exist.
            using (var conn = GetConnection())
            {
                Console.WriteLine($"[Database] Ensuring database exists at: {DbPath}");
                conn.Open();
                using (var tran = conn.BeginTransaction())
                using (var cmd = conn.CreateCommand())
                {
                    // Ensure Experiments table exists. This stores serialized experiment setups or metadata.
                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Experiments (
                                            Id TEXT PRIMARY KEY,
                                            Json TEXT NOT NULL,
                                            CreatedAt TEXT NOT NULL
                                        );";
                    cmd.ExecuteNonQuery();

                    // Ensure modal experiment schemas table exists. Used by SaveModalExperimentSchema/GetModalExperimentSchema
                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS ModalExperimentSchemas (
                                            Id TEXT PRIMARY KEY,
                                            Json TEXT NOT NULL
                                        );";
                    cmd.ExecuteNonQuery();

                    // Commit changes
                    tran.Commit();
                }
            }
        }

        public void SaveModalExperimentSchema(ModalExperimentSchema schema)
        {
            Console.WriteLine($"[Database] Saving modal experiment schema to database id: {schema.ID}");
            if (schema == null) throw new ArgumentNullException(nameof(schema));

            using (var conn = GetConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                using (var cmd = conn.CreateCommand())
                {
                    try
                    {
                        cmd.Transaction = tran;
                        cmd.CommandText = "INSERT OR REPLACE INTO ModalExperimentSchemas (Id, Json) VALUES (@id, @json);";
                        cmd.Parameters.AddWithValue("@id", schema.ID.ToString());
                        var json = JsonConvert.SerializeObject(schema);
                        cmd.Parameters.AddWithValue("@json", json);
                        cmd.ExecuteNonQuery();
                        tran.Commit();
                        Console.WriteLine("[Database] Experiment saved succesfully");
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"[Database] Error occurred while saving modal experiment schema: {ex.Message}");
                    }
                }
            }
        }

        public ModalExperimentSchema GetModalExperimentSchema(Guid id)
        {
            Console.WriteLine($"[Database] Retrieving modal experiment schema from database id: {id}");
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Json FROM ModalExperimentSchemas WHERE Id = @id LIMIT 1;";
                    cmd.Parameters.AddWithValue("@id", id.ToString());
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var json = reader.GetString(0);
                            var schema = JsonConvert.DeserializeObject<ModalExperimentSchema>(json);
                            return schema;
                        }
                    }
                }
            }
            Console.WriteLine($"[Database] No modal experiment schema found with the given ID");
            return null;
        }

        public void ClearModalExperimentSchemas()
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM ModalExperimentSchemas;";
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}