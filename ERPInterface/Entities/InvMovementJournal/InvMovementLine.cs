using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ERPInterface.Entities
{
    public class InvMovementLine
    {
        public string ItemId { get; set; }
        public decimal Qty { get; set; }
        public InventDim InventDim { get; set; }
        public string OffsetAccount { get; set; }
    }
}