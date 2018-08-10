using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using HtmlAgilityPack;
using Microsoft.FSharp.Core;
using Newtonsoft.Json;
using ScrapySharp.Extensions;
using ScrapySharp.Network;
using Serilog;
using Serilog.Core;

namespace FinnScrape
{
    class Program
    {
        static void Main(string[] args)
        {
            
            string cnString;
            string query;
            if (args.Any())
            {
                cnString = args[0];
                query = args[1];
            }
            else
            {
                cnString = "server=localhost;database=master;user id=SA;password=pASADMIN1234";
                query = "engine_effect_from=100&engine_volume_from=600&mileage_to=90000";
            }
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            EnsureDatabase(cnString).Wait();
            FetchData(cnString,query).Wait();

        }

        private static async Task EnsureDatabase(string cnString)
        {
            using (var cn = new SqlConnection(cnString))
            {
                await cn.OpenAsync();
                var exists =
                    await cn.ExecuteScalarAsync<int>("select count(1) from master.sys.databases where name=\'finn\'");
                if (exists == 0)
                {
                    cn.Execute("create database finn");
                    cn.Execute("use finn");
                    cn.Execute(@"create table annonse(
                                id int IDENTITY primary key,
                                finnId nvarchar(255),
                                registrert datetime,
                                CONSTRAINT finnId UNIQUE (finnId))");
                    cn.Execute(@"
                        create table annonseRevisjon(
                            id int IDENTITY primary key,
                            finnId nvarchar(255),
                            merke nvarchar(255),
                            modell nvarchar(255),
                            årsavgift nvarchar(255),
                            kmstand int,
                            årsmodell int,
                            tilstand nvarchar(255),
                            effekt  int,
                            slagvolum int,
                            [type] nvarchar(255),
                            farge nvarchar(255),
                            sistEndret datetime,
                            hash nvarchar(255),
                            pris int,
                            url nvarchar(255),
                            CONSTRAINT fk_annonse FOREIGN KEY (finnId)
                            REFERENCES annonse(finnID)
                    )");
                }
            }
        }
        private static async Task FetchData(string cnString, string query)
        {
            Logger log;
            var allData = new List<Data>();
            var b = new ScrapingBrowser();
            var baseLink = "https://www.finn.no/mc/all/";
            var querylink =$"{baseLink}search.html?{query}&rows=10000&sort=0&page=1";
            Log.Information($"Now scraping finn query {query}", querylink);
            WebPage page = b.NavigateToPage(new Uri(querylink));
            var results = page.Html.CssSelect(".result-item");
            Log.Information("Got {count} items", results.Count());
            foreach (var mcNode in results)
            {
                allData.Add(await FetchDetails(mcNode,baseLink));
            }

            using (var cn = new SqlConnection(cnString))
            {
                await cn.OpenAsync();
                await cn.ExecuteAsync("use finn");

                
                int brandNew = 0;
                int changed = 0;

                Log.Information("Finished scraping {count} objects - now inserting into db", allData.Count);
                foreach (var data in allData)
                {
                    if (data.SistEndret < new DateTime(2000, 1, 1))
                    {
                        data.SistEndret = new DateTime(2000, 1, 1);
                    }

                    if (await cn.ExecuteScalarAsync<int>("select count(1) from annonse where finnId=@finnid",
                            new {data.finnId}) == 0)
                    {
                        await cn.ExecuteAsync("insert into annonse(finnId,registrert)values(@finnId,@sistEndret)",
                            new {data.finnId, data.SistEndret});
                        brandNew++;
                    }

                    var hash = GetHash(data);
                    //Insert Only if changed
                    if (await cn.ExecuteScalarAsync<int>("select count(1) from annonserevisjon where hash=@hash", new {hash}) == 0)
                    {
                        await cn.ExecuteAsync(
                            @"insert into annonseRevisjon(finnId,merke,modell,Årsavgift,Kmstand,Årsmodell,Tilstand,Effekt,Slagvolum,[Type],Farge,SistEndret,hash,pris,url)VALUES(
                                                                @finnId,@merke,@modell,@Årsavgift,@Kmstand,@Årsmodell,@Tilstand,@Effekt,@Slagvolum,@type,@Farge,@SistEndret,@hash,@pris,@url)",
                            new
                            {
                                data.finnId,
                                data.Merke,
                                data.Modell,
                                data.Årsavgift,
                                data.Kmstand,
                                data.Årsmodell,
                                data.Tilstand,
                                data.Effekt,
                                data.Slagvolum,
                                data.Type,
                                data.Farge,
                                data.SistEndret,
                                hash,
                                data.Pris,
                                data.Url
                            });
                        changed++;
                    }
                }

                Log.Information("Inserted {count} brand new", brandNew);
                Log.Information("Inserted {count} changed or new ones", changed);
                Log.Information("Skipped inserting {count} beacause no changes since last revision", allData.Count - changed);
            }

            Log.Information("=====================");
            Log.Warning("Finished - Hit any key to exit");
            
        }

        private static async Task<Data> FetchDetails(HtmlNode mcNode, string baseLink)
        {
            var item = new Data();
            var b=new ScrapingBrowser();
            var detailsLink = mcNode.CssSelect(".linkblock").First();
            item.finnId = detailsLink.Id;
            var url = new Uri($"{baseLink}ad.html?finnkode={detailsLink.Id}");
            var detailsPage = await b.NavigateToPageAsync(url);

            var titleNode = detailsPage.Html.CssSelect(".h1").First();
            var title = titleNode.InnerText.Split(' ').ToList();
            Log.Information("Parsing {id} {title}", item.finnId, titleNode.InnerText);
            var price = detailsPage.Html.SelectSingleNode("//div[@class=\'h1 mtn r-margin\']");
            
            item.Pris = int.Parse(FixString(price.InnerText));
            item.Url = url.ToString();

            item.Merke = title.Count > 0 ? title[0] : "";
            item.Modell = titleNode.InnerText.Substring(titleNode.InnerText.IndexOf(' ') + 1);

            var attributes = detailsPage.Html.SelectNodes("//dt[@data-automation-id=\'key\']");
            foreach (var attribute in attributes)
            {
                var attr = attribute.InnerText.Trim();
                var prop = item.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(p => p.Name.Equals(attr.Replace(".", "")));
                if (prop != null)
                {
                    var data = FixString(attribute.GetNextSibling("dd").InnerText);
                    if (IsNumericType(prop.PropertyType))
                        prop.SetValue(item, int.Parse(data));
                    else
                        prop.SetValue(item, data);
                }
            }

            var sistEndret = detailsPage.Html.SelectNodes("//p//text()[contains(., \'Sist endret\')]").First();
            item.SistEndret = DateTime.Parse(sistEndret.InnerText.Trim().Replace("Sist endret:", "").Trim());
            return item;
        }

        private static string FixString(string input)
        {
            return input.Trim().Replace("km", "").Replace("hk", "").Replace("ccm", "")
                .Replace(Convert.ToChar(160).ToString(), "").Replace(",-", "");
        }
        public static string GetHash(object data)
        {
            using (var md5 = MD5.Create())
            {
                var d = JsonConvert.SerializeObject(data);
                var hash=md5.ComputeHash(Encoding.UTF8.GetBytes(d));

                StringBuilder sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < hash.Length; i++)
                {
                    sBuilder.Append(hash[i].ToString("x2"));
                }

                // Return the hexadecimal string.
                return sBuilder.ToString();
            }
        }

        public static bool IsNumericType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }
}
