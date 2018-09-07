using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebSite.Models
{
    public class ChartModel
    {
        public readonly List<int> xs;
        public readonly List<int> ys;

        public ChartModel(List<int> xs, List<int> ys)
        {
            this.xs = xs;
            this.ys = ys;
        }
    }
}