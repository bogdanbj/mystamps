using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.IO;

namespace mystamps.Utils
{
    internal class DbHelper
    {
        private SqliteConnection _connection;
        private string _dbName;


        SqliteConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    var dbDir = Path.Combine(Environment.CurrentDirectory, "database");
                    //Directory.CreateDirectory(dbDir);
                    var dbPath = Path.Combine(dbDir, _dbName);
                    _connection = new SqliteConnection($"Data Source={dbPath}");
                    _connection.Open();
                }
                return _connection;
            }
        }

        public DbHelper(string dbName)
        {
            _dbName = dbName;
        }

        internal void GetStampByReferenceNumber(Stamp stamp, string referenceNumber)
        {
            var cmd = this.Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT ReferenceNumber, Url, ImageFileName, PostalAdministration, Title, Denomination, DateOfIssue, Series, SeriesYear, Printer, Quantity, Perforation, Creators, HistoricalNotice, PostalNumber
                FROM stamps
                WHERE ReferenceNumber = $ref;
            ";
            cmd.Parameters.AddWithValue("$ref", referenceNumber);
            using SqliteDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                int i;
                i = reader.GetOrdinal("ReferenceNumber"); stamp.ReferenceNumber = reader.IsDBNull(i) ? null : reader.GetString(i);
                i = reader.GetOrdinal("Url"); stamp.Url = reader.IsDBNull(i) ? null : reader.GetString(i);
                i = reader.GetOrdinal("ImageFileName"); stamp.Image = reader.IsDBNull(i) ? null : reader.GetString(i);
                i = reader.GetOrdinal("PostalAdministration"); stamp.PostalAdministration = reader.IsDBNull(i) ? null : reader.GetString(i);
                i = reader.GetOrdinal("Title"); stamp.Title = reader.IsDBNull(i) ? null : reader.GetString(i);
                i = reader.GetOrdinal("Denomination"); stamp.Denomination = reader.IsDBNull(i) ? null : reader.GetString(i);
                i = reader.GetOrdinal("DateOfIssue"); stamp.DateOfIssue = reader.IsDBNull(i) ? null : reader.GetString(i);
                i = reader.GetOrdinal("Series"); stamp.Series = reader.IsDBNull(i) ? null : reader.GetString(i);
                i = reader.GetOrdinal("SeriesYear"); stamp.SeriesYear = reader.IsDBNull(i) ? null : reader.GetString(i);
                i = reader.GetOrdinal("Printer"); stamp.Printer = reader.IsDBNull(i) ? null : reader.GetString(i);
                i = reader.GetOrdinal("Quantity"); stamp.Quantity = reader.IsDBNull(i) ? null : reader.GetString(i);
                i = reader.GetOrdinal("Perforation"); stamp.Perforation = reader.IsDBNull(i) ? null : reader.GetString(i);
                i = reader.GetOrdinal("Creators"); stamp.Creators = reader.IsDBNull(i) ? new List<string>() : reader.GetString(i).Split(';', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                i = reader.GetOrdinal("HistoricalNotice"); stamp.HistoricalNotice = reader.IsDBNull(i) ? null : reader.GetString(i);
                i = reader.GetOrdinal("PostalNumber"); stamp.PostalNumber = reader.IsDBNull(i) ? null : reader.GetString(i);
                //i = reader.GetOrdinal("ReferenceNumber"); stamp.ReferenceNumber = reader.GetString(i);
                //stamp.Url = reader.IsDBNull(1) ? null : reader.GetString(1);
                //stamp.Image = reader.IsDBNull(2) ? null : reader.GetString(2);
                //stamp.PostalAdministration = reader.IsDBNull(3) ? null : reader.GetString(3);
                //stamp.Title = reader.IsDBNull(4) ? null : reader.GetString(4);
                //stamp.Denomination = reader.IsDBNull(5) ? null : reader.GetString(5);
                //stamp.DateOfIssue = reader.IsDBNull(6) ? null : reader.GetString(6);
                //stamp.Printer = reader.IsDBNull(7) ? null : reader.GetString(7);
                //stamp.Quantity = reader.IsDBNull(8) ? null : reader.GetString(8);
                //stamp.Perforation = reader.IsDBNull(9) ? null : reader.GetString(9);
                //stamp.Creators = reader.IsDBNull(10) ? new List<string>() : reader.GetString(10).Split(';', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                //stamp.HistoricalNotice = reader.IsDBNull(11) ? null : reader.GetString(11);
                //stamp.PostalNumber = reader.IsDBNull(12) ? null : reader.GetString(12);
                //stamp.Series = reader.IsDBNull(12) ? null : reader.GetString(13);
                //stamp.SeriesYear = reader.IsDBNull(12) ? null : reader.GetString(14);



            }
            return;
        }

        internal void SaveStamp(Stamp stamp)
        {
            var cmd = this.Connection.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO stamps (
                    ReferenceNumber, Url, ImageFileName, PostalAdministration, Title, Denomination, DateOfIssue, Series, SeriesYear, Printer, Quantity, Perforation, Creators, HistoricalNotice, PostalNumber
                ) VALUES (
                    $ReferenceNumber, $Url, $ImageFileName, $PostalAdministration, $Title, $Denomination, $DateOfIssue, $Series, $SeriesYear, $Printer, $Quantity, $Perforation, $Creators, $HistoricalNotice, $PostalNumber
                );
            ";
            cmd.Parameters.AddWithValue("$ReferenceNumber", stamp.ReferenceNumber ?? "");
            cmd.Parameters.AddWithValue("$Url", stamp.Url ?? "");
            cmd.Parameters.AddWithValue("$ImageFileName", stamp.Image ?? "");
            cmd.Parameters.AddWithValue("$PostalAdministration", stamp.PostalAdministration ?? "");
            cmd.Parameters.AddWithValue("$Title", stamp.Title ?? "");
            cmd.Parameters.AddWithValue("$Denomination", stamp.Denomination ?? "");
            cmd.Parameters.AddWithValue("$DateOfIssue", stamp.DateOfIssue ?? "");
            cmd.Parameters.AddWithValue("$Series", stamp.Series ?? "");
            cmd.Parameters.AddWithValue("$SeriesYear", stamp.SeriesYear ?? "");
            cmd.Parameters.AddWithValue("$Printer", stamp.Printer ?? "");
            cmd.Parameters.AddWithValue("$Quantity", stamp.Quantity ?? "");
            cmd.Parameters.AddWithValue("$Perforation", stamp.Perforation ?? "");
            cmd.Parameters.AddWithValue("$Creators", stamp.Creators != null ? string.Join("; ", stamp.Creators) : "");
            cmd.Parameters.AddWithValue("$HistoricalNotice", stamp.HistoricalNotice ?? "");
            cmd.Parameters.AddWithValue("$PostalNumber", stamp.PostalNumber ?? "");
            cmd.ExecuteNonQuery();
        }

        internal void SaveError(string url, string errorMessage)
        {
            var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO errors (Url, ErrorMessage, ErrorTime)
                VALUES ($Url, $ErrorMessage, $ErrorTime);
            ";
            cmd.Parameters.AddWithValue("$Url", url ?? "");
            cmd.Parameters.AddWithValue("$ErrorMessage", errorMessage ?? "");
            cmd.Parameters.AddWithValue("$ErrorTime", DateTime.UtcNow.ToString("o"));
            cmd.ExecuteNonQuery();
        }

        internal void CreateStampsDatabase()
        {
            var cmd = Connection.CreateCommand();
            //cmd.CommandText = @"
            //    DROP TABLE IF EXISTS stamps;
            //    CREATE TABLE IF NOT EXISTS stamps (
            //        ReferenceNumber TEXT PRIMARY KEY,
            //        PostalAdministration TEXT,
            //        Title TEXT,
            //        Denomination TEXT,
            //        DateOfIssue TEXT,
            //        Url TEXT,
            //        ImageFileName TEXT,
            //        Printer TEXT,
            //        Quantity TEXT,
            //        Perforation TEXT,
            //        Creators TEXT,
            //        HistoricalNotice TEXT,
            //        PostalNumber TEXT
            //    );
            //";
            //cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                DROP TABLE IF EXISTS errors;
                CREATE TABLE IF NOT EXISTS errors (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Url TEXT,
                    ErrorMessage TEXT,
                    ErrorTime TEXT
                );
            ";
            cmd.ExecuteNonQuery();

        }
    }
}
