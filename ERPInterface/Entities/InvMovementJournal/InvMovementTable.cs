﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ERPInterface.Entities.InvMovementJournal
{
    public class InvMovementTable
    {
        public string MovementType { get; set; }
        public string Description { get; set; }
        public List<InvMovementLine> ListMovementLine { get; set; }
    }
}