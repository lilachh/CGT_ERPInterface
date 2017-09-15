using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ERPInterface.Entities.SalesPackingSlip
{
    public class SalesCreditNote
    {
        public string  CustAccount { get; set; }
        public List<SalesLine> LstSalesLine { get; set; }
    }
}