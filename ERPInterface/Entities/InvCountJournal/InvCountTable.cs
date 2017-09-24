using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ERPInterface.Entities
{
    public class InvCountTable
    {
        public string Description { get; set; }
        public List<InvCountLine> LstCountLine { get; set; }
        public string DateYMD { get; set; }
    }
}