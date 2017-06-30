using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ERPInterface.Entities
{
    public class InventDim
    {
        public string InventLocationId { get; set; }
        public string inventBatchId { get; set; }
        public string wMsLocationId { get; set; }
        public string wMSPallentId { get; set; }
        public string inventSerialId { get; set; }
    }
}