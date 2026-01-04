using JfYu.Data.Model;
using System.ComponentModel.DataAnnotations;

namespace NGA.Models.Entity
{
    public class Replay : BaseEntity
    {
        [MaxLength(30)]
        public string Tid { get; set; }

        [MaxLength(30)]
        public string Uid { get; set; }

        [MaxLength(30)]
        public string UName { get; set; }

        [MaxLength(30)]
        public string Pid { get; set; }
        public int Sort { get; set; }
        public int Support { get; set; }
        public int Oppose { get; set; }

        [MaxLength(40)]
        public string Moblie { get; set; }

        [MaxLength(50)]
        public string PostDate { get; set; }
        public string Content { get; set; }
        [MaxLength(50)]
        public string QuotePid { get; set; }

    }

}
