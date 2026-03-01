using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace mystamps
{
    internal class StampList : List<Stamp>
    {
        internal void PrintAll()
        {
            foreach (var stamp in this)
            {
                stamp.Print();
            }
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

    }
}
