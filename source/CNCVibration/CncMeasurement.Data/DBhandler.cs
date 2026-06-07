using CncMeasurement.Core.Interfaces;
using CncMeasurement.Core.models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json; // Swapped to Newtonsoft.Json
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

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
        public DatabaseController(string path)
        {
            DBpath = path;
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


       

        public MeasurementMetadata GetMeasurementByID(int measurementID)
        {
            return null;
        }
    }
}