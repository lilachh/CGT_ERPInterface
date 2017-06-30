using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ERPInterface.Entities
{
    public class SalesTable
    {
        public string SalesId { get; set; }
        public List<SalesLine> LstSalesLine { get; set; }
    }
}