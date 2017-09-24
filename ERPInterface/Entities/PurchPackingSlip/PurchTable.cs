using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ERPInterface.Entities
{
    public class PurchTable
    {
        public string PurchId { get; set; }
        public List<PurchLine> LstPurchLine { get; set; }
        public bool Receive { get; set; }
        public string PackingSlipId { get; set; }
        public string DateYMD { get; set; }
    }

    public class PurchCreditNote
    {
        public string PurchId { get; set; }
        public string ItemId { get; set; }
        public InventDim InventDim { get; set; }
        public decimal Qty { get; set; }
        public string PackingSlipId { get; set; }
        public string DateYMD { get; set; }
    }



}