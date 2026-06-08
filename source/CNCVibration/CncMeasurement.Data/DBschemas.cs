using CncMeasurement.Core.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CncMeasurement.Data
{
    public static class DBschemas
    {
        // Add SQL for creating ModalExperimentSchemas table
        public const string CreateExperimentTable = "CREATE TABLE IF NOT EXISTS ModalExperimentSchemas (Id TEXT PRIMARY KEY, Json TEXT NOT NULL);";
    }
}

