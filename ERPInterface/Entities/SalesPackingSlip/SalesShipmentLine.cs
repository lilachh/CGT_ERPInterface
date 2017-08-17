using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ERPInterface.Entities
{
    public class SalesShipmentLine
    {
        public string SalesId { get; set; }
        public string LineId { get; set; }
        public string ItemId { get; set; }
        public decimal Qty { get; set; }
        public InventDim InventDim { get; set; }
    }
    class Del_SalesShipmentLine
    {
        public string InvOutPutOrder { get; set; }
        public string ItemId { get; set; }
        public decimal Qty { get; set; }
        public InventDim InventDim { get; set; }
    }
}