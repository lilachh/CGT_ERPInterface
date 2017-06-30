using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ERPInterface.Entities
{
    public class InvJournalLine
    {
        public string ItemId { get; set; }
        public decimal Qty { get; set; }
        public InventDim InventDimFrom { get; set; }
        public InventDim InventDimTo { get; set; }
    }
}