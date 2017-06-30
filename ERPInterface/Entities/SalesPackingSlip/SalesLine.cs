using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ERPInterface.Entities.SalesPackingSlip
{
    public class SalesLine
    {
        public string LineNum { get; set; }
        public string ItemId { get; set; }
        public decimal Qty { get; set; }
        public InventDim InventDim { get; set; }
    }
}