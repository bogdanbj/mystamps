
using HtmlAgilityPack;
using mystamps;
using mystamps.Utils;

namespace MyStamps
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine(Environment.CurrentDirectory);

            List<string> urls = new List<string>();

            if (args.Length == 0)
            {
                // Try to read links from links.txt
                var linksPath = Path.Combine(Environment.CurrentDirectory, "docs\\links.txt");
                if (File.Exists(linksPath))
                {
                    urls = File.ReadAllLines(linksPath)
                               .Select(line => line.Trim())
                               .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith("#"))
                               .ToList();
                }
                else
                {
                    Console.WriteLine("Usage: MyStamps.exe <URL>");
                    Console.WriteLine("Example: MyStamps.exe https://example.com");
#if DEBUG
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
#endif
                    return;
                }
            }
            else
            {
                urls.Add(args[0]);
            }

            try
            {
                foreach (var url in urls)
                {
                    try
                    {
                        await ProcessUrl(url);
                        //DbHelper db = new DbHelper("stamps.db");
                        //db.CreateStampsDatabase();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing {url}: {ex.Message}");
                        // Optionally log ex.StackTrace or other details here
                        continue; // Move to the next URL
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
#if DEBUG
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
#endif
            }
        }

        static async Task ProcessUrl(string url)
        {
            HtmlParser parser = new HtmlParser();

            // Get HTML and response using HttpHelper
            string html = await HttpHelper.GetHtml(url);

            // Find the specific table that contains the stamp data by looking for the table with the header "Ref."
            HtmlNodeCollection? stampRows = parser.GetStampRows(html);

            // Loop through each data row and extract stamp information
            var stamps = new StampList();
            foreach (var row in stampRows)
            {
                try
                {
                    Stamp stamp = new Stamp();
                    parser.ParseStampRow(stamp, row, url);
                    Console.WriteLine(stamp);
                    if (stamp != null)
                    {
                        stamps.Add(stamp);
                        if (!string.IsNullOrEmpty(stamp.Url))
                        {
                            string stampHtml = await HttpHelper.GetHtml(stamp.Url);

                            parser.ParseStampPage(stamp, stampHtml);

                        }
                        //Console.WriteLine(stamp.Image);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing stamp row: {ex.Message}");
                    // Optionally log ex.StackTrace or other details here
                    continue; // Move to the next stamp row
                }
            }
            //Console.WriteLine(stamps.ToJson());
            Console.WriteLine("=== End Page ===");
            Console.WriteLine();
            Console.WriteLine();

            return;

        }


    }
}
