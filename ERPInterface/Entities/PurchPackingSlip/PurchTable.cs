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
    }
}