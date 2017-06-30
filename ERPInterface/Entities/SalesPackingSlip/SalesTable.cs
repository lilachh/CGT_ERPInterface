using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ERPInterface.Entities.SalesPackingSlip
{
    public class SalesTable
    {
        public string SalesId { get; set; }
        public List<SalesLine> LstSalesLine { get; set; }
    }
}