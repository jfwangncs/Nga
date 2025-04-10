using jfYu.Core.Data.Model;
using System.ComponentModel.DataAnnotations;

namespace NGA.Models
{
    public class ReplayHis : BaseEntity
    {
       
        [MaxLength(30)]
        public string Tid { get; set; }
        [MaxLength(30)]
        public string Uid { get; set; }
        [MaxLength(30)]
        public string Pid { get; set; }
        [MaxLength(100)]
        public string PostDate { get; set; }
        public string Content { get; set; }

    }

}
