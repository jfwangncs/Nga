using jfYu.Core.Data.Model;
using System.ComponentModel.DataAnnotations;

namespace NGA.Models
{
    public class Log : BaseEntity
    {
        [MaxLength(8000)]
        public string Msg { get; set; }

        public string Trace { get; set; }
        [MaxLength(500)]
        public string Info { get; set; }
        [MaxLength(50)]
        public string Type { get; set; }
    }
}
