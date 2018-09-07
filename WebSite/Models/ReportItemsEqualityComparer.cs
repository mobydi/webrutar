using System.Collections.Generic;
using RuTarCommon;

namespace WebSite.Models
{
    public class ReportItemsEqualityComparer : IEqualityComparer<ReportItem>
    {
        public static readonly ReportItemsEqualityComparer Instance = new ReportItemsEqualityComparer();

        public bool Equals(ReportItem x, ReportItem y)
        {
            return x.PostId == y.PostId;
        }

        public int GetHashCode(ReportItem obj)
        {
            return obj.PostId;
        }
    }
}