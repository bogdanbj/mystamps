using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mystamps.Utils
{
    internal class HtmlParser
    {
        internal HtmlNodeCollection GetStampRows(string html)
        {
            // Load HTML into HtmlAgilityPack
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Find the specific table that contains the stamp data by looking for the table with the header "Ref."
            HtmlNode stampTable = doc.DocumentNode
                .SelectSingleNode("//table[@width='100%'][.//td[contains(., 'Ref.')]]");
            if (stampTable == null)
            {
                Console.WriteLine("No stamp table found on this page.");
                return null;
            }
            Console.WriteLine("Found stamp collection table!");

            var dataRows = stampTable.SelectNodes(".//tr[count(td)=5 and not(td[1]//b[contains(., 'Ref.')])]");
            if (dataRows == null || dataRows.Count == 0)
            {
                Console.WriteLine("No stamp data rows found in the table.");
                return null;
            }
            Console.WriteLine($"\nFound {dataRows.Count} stamp entries:\n");
            return dataRows;

        }
    
        internal void ParseStampRow(Stamp stamp, HtmlNode row, string baseUrl)
        {
            var cells = row.SelectNodes(".//td");
            if (cells == null || cells.Count < 5)
            {
                Console.WriteLine("Warning: Row does not have enough cells to parse stamp data.");
                return;
            }
            stamp.ReferenceNumber = HttpHelper.CleanHtmlText(cells[0].InnerText);
            stamp.PostalAdministration = HttpHelper.CleanHtmlText(cells[2].InnerText);
            stamp.Title = HttpHelper.CleanHtmlText(cells[3].InnerText);
            stamp.DateOfIssue = HttpHelper.CleanHtmlText(cells[4].InnerText);
            HtmlNode aTag = cells[1].SelectSingleNode(".//a[@href]");
            stamp.Url = HttpHelper.GetUri(aTag, baseUrl);
            return;
        }

        internal void ParseStampPage(Stamp stamp, string html) 
        {
            DbHelper db = new DbHelper("stamps.db");
            db.GetStampByReferenceNumber(stamp, stamp.ReferenceNumber);

            // Load HTML into HtmlAgilityPack
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            
            HtmlNode 
                nodeScript,
                nodeText,
                nodeTr,
                nodeTd,
                nodeA;
            HtmlNodeCollection nodeCollection;

            // 1. Image Image URL from process_link() function in <script>
            nodeScript = doc.DocumentNode.SelectSingleNode("//script[contains(text(),'process_link')]");
            if (nodeScript != null)
            {
                var script = nodeScript.InnerText;
                var imgUrlStart = script.IndexOf("window.open(\"") + "window.open(\"".Length;
                var imgUrlEnd = script.IndexOf("\"", imgUrlStart);
                if (imgUrlStart > 0 && imgUrlEnd > imgUrlStart)
                {
                    var imageUrl = script.Substring(imgUrlStart, imgUrlEnd - imgUrlStart);
                    // Download the image and save to "pictures" folder
                    var picturesDir = Path.Combine(Environment.CurrentDirectory, "pictures");
                    Directory.CreateDirectory(picturesDir);
                    
                    var fileName = Path.GetFileName(new Uri(imageUrl).LocalPath);
                    var filePath = Path.Combine(picturesDir, fileName);
                    try { 
                    using (var httpClient = new HttpClient())
                    {
                        var imageBytes = httpClient.GetByteArrayAsync(imageUrl).Result;
                        File.WriteAllBytes(filePath, imageBytes);
                    }
                    stamp.Image = fileName;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error downloading image for {stamp.ReferenceNumber}: {ex.Message}");
                        new DbHelper("stamps.db").SaveError(imageUrl, ex.Message);

                        // Optionally: stamp.Image = null;
                    }
                }
            }

            // 2. Postal Administration
            nodeText = doc.DocumentNode.SelectSingleNode("//b[contains(text(),'Postal Administration:')]/following-sibling::text()");
            stamp.PostalAdministration = HttpHelper.CleanHtmlText(nodeText?.InnerText);

            // 3. Title
            nodeText = doc.DocumentNode.SelectSingleNode("//b[contains(text(),'Title:')]/following-sibling::text()");
            stamp.Title = HttpHelper.CleanHtmlText(nodeText?.InnerText);

            // 4. Denomination
            nodeText = doc.DocumentNode.SelectSingleNode("//b[contains(text(),'Denomination:')]/following-sibling::text()");
            stamp.Denomination = HttpHelper.CleanHtmlText(nodeText?.InnerText);

            // 5. Date of Issue
            nodeText = doc.DocumentNode.SelectSingleNode("//b[contains(text(),'Date of Issue:')]/following-sibling::text()");
            stamp.DateOfIssue = HttpHelper.CleanHtmlText(nodeText?.InnerText);

            //6. Series
            nodeTd = doc.DocumentNode.SelectSingleNode("//td[./b[contains(text(),'Series:')]]/following-sibling::td[1]");
            stamp.Series = HttpHelper.CleanHtmlText(nodeTd?.InnerText);

            //7. Series Year (inclusive dates)
            nodeTd = doc.DocumentNode.SelectSingleNode("//td[./b[contains(text(),'Series Year')]]/following-sibling::td[1]");
            stamp.SeriesYear = HttpHelper.CleanHtmlText(nodeTd?.InnerText);

            // 8. Printer and Quantity
            //node = doc.DocumentNode.SelectSingleNode("//b[contains(text(),'Printer/Quantity:')]/parent::td/following-sibling::td");
            nodeTr = doc.DocumentNode.SelectSingleNode("//tr[td/b[contains(text(),'Printer/Quantity:')]]");
            if (nodeTr != null)
            {
                // Printer is in <a> tag, quantity is in next <tr>
                nodeA = nodeTr.SelectSingleNode("./td[2]//a");
                stamp.Printer = HttpHelper.CleanHtmlText(nodeA?.InnerText);

                nodeText = nodeTr.SelectSingleNode("./following-sibling::tr[1]/td[1]");
                if (string.IsNullOrEmpty(HttpHelper.CleanHtmlText(nodeText?.InnerText)))
                {
                    nodeText = nodeTr.SelectSingleNode("./following-sibling::tr[1]/td[2]");
                    stamp.Quantity = HttpHelper.CleanHtmlText(nodeText?.InnerText);
                }
                else
                    stamp.Quantity = null;
            }

            // 9. Perforation
            nodeTd = doc.DocumentNode.SelectSingleNode("//b[contains(text(),'Perforation:')]/parent::td/following-sibling::td");
            stamp.Perforation = HttpHelper.CleanHtmlText(nodeTd?.InnerText);

            // 10. Creators
            nodeCollection = doc.DocumentNode.SelectNodes("//b[contains(text(),'Creator(s):')]/parent::td/following-sibling::td//a");
            if (nodeCollection != null)
                stamp.Creators = nodeCollection.Select(n => HttpHelper.CleanHtmlText(n.InnerText)).ToList();

            // 11. Historical Notice
            nodeTd = doc.DocumentNode.SelectSingleNode("//b[contains(text(),'Historical Notice:')]/parent::td/following-sibling::td");
            stamp.HistoricalNotice = HttpHelper.CleanHtmlText(nodeTd?.InnerText);

            // 12. Postal Number (from Source)
            nodeTd = doc.DocumentNode.SelectSingleNode("//b[contains(text(),'Source:')]/parent::td/following-sibling::td");
            stamp.PostalNumber = HttpHelper.CleanHtmlText(nodeTd?.InnerText);

            //return details;
            db.SaveStamp(stamp);


        }
    }
}
