using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace mystamps
{
    public class Stamp
    {
        public string ReferenceNumber { get; set; }
        public string Url { get; set; }
        public string Image { get; set; }
        public string PostalAdministration { get; set; }
        public string Title { get; set; }
        public string Denomination { get; set; }
        public string DateOfIssue { get; set; }
        public string Series { get; set; }
        public string SeriesYear { get; set; }
        public string Printer { get; set; }
        public string Quantity { get; set; }
        public string Perforation { get; set; }
        public List<string> Creators { get; set; }
        public string HistoricalNotice { get; set; }
        public string PostalNumber { get; set; }

        public void Print()
        {
            Console.WriteLine($"{ReferenceNumber} {PostalAdministration} {Title} {DateOfIssue} {Url}");
        }
        internal string ToJson()
        {
            return JsonSerializer.Serialize(
                this,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
        }
        public override string ToString()
        {
            // Try to extract year from DateOfIssue
            string year = "";
            if (!string.IsNullOrEmpty(DateOfIssue))
            {
                // Try to find a 4-digit year in the string
                var match = System.Text.RegularExpressions.Regex.Match(DateOfIssue, @"\b\d{4}\b");
                if (match.Success)
                    year = match.Value;
            }
            return $"{ReferenceNumber}, {PostalAdministration}, {Title}, {year}";
        }

    }
}
