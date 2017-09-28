using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ERPInterface.Entities
{
    public class SalesShipmentTable
    {
        public string ShipmentId { get; set; }
        //public string EmpId { get; set; }
        public List<SalesShipmentLine> LstShipmentLine { get; set; }
        public string DateYMD { get; set; }
    }

    //public class SalesShipmentTable
    //{
    //    public string ShipmentId { get; set; }
    //    //public string EmpId { get; set; }
    //    public List<SalesShipmentLine> LstShipmentLine { get; set; }
        
    //}
}