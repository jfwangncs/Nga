using JfYu.Data.Model;
using System.ComponentModel.DataAnnotations;

namespace NGA.Models.Entity
{
    public class User : BaseEntity
    {
        [MaxLength(30)]
        public string Uid { get; set; }
        [MaxLength(50)]
        public string UserName { get; set; }
        [MaxLength(50)]
        public string Group { get; set; }
        [MaxLength(40)]
        public string Regdate { get; set; }
        [MaxLength(100)]
        public string Avatar { get; set; }
    }
}
