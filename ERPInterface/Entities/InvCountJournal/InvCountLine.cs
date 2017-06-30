using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ERPInterface.Entities.InvCountJournal
{
    public class InvCountLine
    {
        public string ItemId { get; set; }
        public decimal Qty { get; set; }
        public InventDim InventDim { get; set; }
    }
}