using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ERPInterface.Entities
{
    public class InvJournalTable
    {
        public string Description { get; set; }
        public List<InvJournalLine> LstJournalLine { get; set; }
        public string DateYMD { get; set; }
    }
}