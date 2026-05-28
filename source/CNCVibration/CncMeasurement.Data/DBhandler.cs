using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CncMeasurement.Core.models;
using CncMeasurement.Core.Interfaces;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json; // Swapped to Newtonsoft.Json

namespace CncMeasurement.Data
{
    

    public class DatabaseController : IDatabaseController
    {
        private readonly string _connectionString;

        public DatabaseController(string ConnectionString)
        {
            _connectionString = ConnectionString;
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

        public void InitializeCollections()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Measurements (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp TEXT NOT NULL,
                    Graphs TEXT,
                    Description TEXT,
                    Notes TEXT
                 )";
                command.ExecuteNonQuery();
            }

        }

        public void ClearDatabase()
        {
            throw new NotImplementedException();
        }

        public void AddMeasurementEntry(MeasurementMetadata measuredData)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();

                // 1. Get all public properties of the Measurement class
                var properties = typeof(MeasurementMetadata).GetProperties();

                var columnNames = new List<string>();
                var parameterNames = new List<string>();

                // 2. Loop through properties to build the dynamic query and parameters
                foreach (var prop in properties)
                {
                    string propName = prop.Name;

                    // Skip the Primary Key (let SQLite auto-increment it)
                    if (propName.Equals("Id", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string paramName = "$" + propName;
                    columnNames.Add(propName);
                    parameterNames.Add(paramName);

                    // Extract the actual value from the object
                    object rawValue = prop.GetValue(measuredData);
                    object dbValue;

                    // 3. Handle special cases based on property type
                    if (prop.PropertyType == typeof(GraphMetadata[]))
                    {
                        // Serialize the graph array to JSON using Newtonsoft
                        dbValue = rawValue != null
                            ? JsonConvert.SerializeObject((GraphMetadata[])rawValue)
                            : "[]"; // Default to empty JSON array if null
                    }
                    else if (prop.PropertyType == typeof(DateTime))
                    {
                        // Force DateTime to strict ISO 8601 string format
                        dbValue = ((DateTime)rawValue).ToString("o");
                    }
                    else
                    {
                        // For primitives (strings, ints, doubles), take the value as-is
                        dbValue = rawValue;
                    }

                    // 4. Add the parameter to the command, converting C# null to DBNull
                    command.Parameters.AddWithValue(paramName, dbValue ?? DBNull.Value);
                }

                // 5. Construct the final SQL INSERT string
                string columnsSql = string.Join(", ", columnNames);
                string paramsSql = string.Join(", ", parameterNames);

                command.CommandText = $"INSERT INTO Measurements ({columnsSql}) VALUES ({paramsSql})";

                // Execute!
                command.ExecuteNonQuery();
            }
        }

        public List<BriefMeasurementInfo> GetMeasurementSummaries()
        {
            var summaries = new List<BriefMeasurementInfo>();

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();

                // SELECT only the 3 columns that match your BriefMeasurementInfo class
                command.CommandText = "SELECT Id, Timestamp, Description FROM Measurements";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var summary = new BriefMeasurementInfo
                        {
                            // Map the data directly to your new DTO class
                            ID = reader.GetInt32(0),

                            // Parse the Timestamp
                            Timestamp = DateTime.Parse(reader.GetString(1)),

                            // Safely handle the Description, which might be NULL
                            Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                        };

                        summaries.Add(summary);
                    }
                }
            }

            return summaries;
        }

        public MeasurementMetadata GetMeasurementByID(int measurementID)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();

                // Use $id parameter to safely query a specific row
                command.CommandText = "SELECT * FROM Measurements WHERE Id = $id";
                command.Parameters.AddWithValue("$id", measurementID);

                using (var reader = command.ExecuteReader())
                {
                    // We use 'if' instead of 'while' because IDs are unique; there will only be one result.
                    if (reader.Read())
                    {
                        var measurement = new MeasurementMetadata();
                        var properties = typeof(MeasurementMetadata).GetProperties();

                        // Dynamically map the columns to the C# properties
                        foreach (var prop in properties)
                        {
                            int ordinal;
                            try
                            {
                                ordinal = reader.GetOrdinal(prop.Name);
                            }
                            catch (IndexOutOfRangeException)
                            {
                                continue; // Column not found in DB, skip
                            }

                            if (reader.IsDBNull(ordinal))
                            {
                                if (prop.PropertyType == typeof(GraphMetadata[]))
                                {
                                    prop.SetValue(measurement, Array.Empty<GraphMetadata>());
                                }
                                continue;
                            }

                            object dbValue = reader.GetValue(ordinal);

                            // Handle special type conversions
                            if (prop.PropertyType == typeof(GraphMetadata[]))
                            {
                                string json = (string)dbValue;
                                // Deserialize the JSON string back to an array using Newtonsoft
                                var graphs = JsonConvert.DeserializeObject<GraphMetadata[]>(json);
                                prop.SetValue(measurement, graphs);
                            }
                            else if (prop.PropertyType == typeof(DateTime))
                            {
                                DateTime dt = DateTime.Parse((string)dbValue);
                                prop.SetValue(measurement, dt);
                            }
                            else
                            {
                                object convertedValue = Convert.ChangeType(dbValue, prop.PropertyType);
                                prop.SetValue(measurement, convertedValue);
                            }
                        }

                        return measurement; // Return the fully populated object
                    }
                }
            }

            // If reader.Read() is false, the ID doesn't exist in the database
            return null;
        }
    }
}