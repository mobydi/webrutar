using System;
using Microsoft.WindowsAzure;

namespace RuTarCommon
{
    public static class ContextFactory
    {
        public static ReportContext GetReportContext()
        {
            var dbConnString = CloudConfigurationManager.GetSetting("RuTarDbConnectionString");
            return new ReportContext(dbConnString);
        }
    }
}
