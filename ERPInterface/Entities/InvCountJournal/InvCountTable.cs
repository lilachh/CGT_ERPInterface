using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ERPInterface.Entities.InvCountJournal
{
    public class InvCountTable
    {
        public string Description { get; set; }
        public List<InvCountLine> LstCountLine { get; set; }
    }
}