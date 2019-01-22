using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QiitaTrendReader
{
    public class Item
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Uuid { get; set; }
        public bool IsNewArrival { get; set; }
        public DateTime CreatedAt { get; set; }

        public Item(string title, string author, string uuid, string createdAt, bool isNewArrival)
        {
            Title = title;
            Author = author;
            Uuid = uuid;
            CreatedAt = DateTime.Parse(createdAt);
            IsNewArrival = isNewArrival;
        }
    }
}
