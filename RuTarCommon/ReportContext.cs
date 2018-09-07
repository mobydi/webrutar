using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace RuTarCommon
{
    public class ReportContext : DbContext
    {
        internal ReportContext(string connString)
            : base(connString)
        {
        }

        public DbSet<Report> Reports { get; set; }
        public DbSet<ReportItem> ReportItems { get; set; }
        public DbSet<ReportItemMax> ReportItemMaxs { get; set; }
    }

    public class Report
    {
        [Key]
        public int ReportId { get; set; }
        [StringLength(256)]
        public string Title { get; set; }
        [StringLength(256)]
        public string UserId { get; set; }
        public int CurrentOffset { get; set; }
        [StringLength(256)]
        public string AccessToken { get; set; }
        [StringLength(256)]
        public string State { get; set; }
        public bool Finished { get; set; }
    }

    public class ReportItem
    {
        [Key]
        public int Id { get; set; }
        [Index]
        public int ReportId { get; set; }
        public int PostId { get; set; } 
        public int PostLength { get; set; }
        public int LikesCount { get; set; }
    }

    public class ReportItemMax
    {
        [Key]
        public int Id { get; set; }
        [Index]
        public int ReportId { get; set; }
        public int PostLength { get; set; }
        public int LikesCount { get; set; }
        public int PostId { get; set; } 
    }
}
