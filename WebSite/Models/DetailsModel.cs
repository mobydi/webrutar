using RuTarCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebSite.Models
{
    public class DetailsModel
    {
        public Report Report { get; private set; }
        public string Top { get; private set; }

        public DetailsModel(Report report, string top)
        {
            this.Report = report;
            this.Top = top;
        }
    }
}