using System;
using System.Collections.Generic;
using System.Text;

namespace TradeIS.Models
{
    public class Section
    {
        public int Id { get; set; }

        public int TradePointId { get; set; }

        public string Name { get; set; }

        public int Floor { get; set; }

        public string ManagerName { get; set; }
    }
}
