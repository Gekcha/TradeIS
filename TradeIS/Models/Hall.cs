using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace TradeIS.Models
{
    public class Hall
    {
        [DisplayName("Название зала")]
        public string Name { get; set; }
        [DisplayName("Торговая точка")]
        public string TradePoint { get; set; }
        [DisplayName("Количество продавцов")]
        public int SellersCount { get; set; }
    }
}
