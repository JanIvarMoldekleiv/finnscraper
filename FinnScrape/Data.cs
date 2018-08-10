using System;

namespace FinnScrape
{
    public class Data
    {
        public string finnId { get; set; }
        public string Merke { get; set; }
        public string Modell { get; set; }
        public string Årsavgift { get; set; }
        public int Kmstand { get; set; }
        public int Årsmodell { get; set; }
        public string Tilstand { get; set; }
        public int Effekt { get; set; }
        public int Slagvolum { get; set; }
        public string Type { get; set; }
        public string Farge { get; set; }
        public DateTime SistEndret { get; set; }
        public int Pris { get; set; }
        public string Url { get; set; }
    }
}