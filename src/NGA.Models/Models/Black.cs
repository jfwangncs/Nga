using JfYu.Data.Model;
using System.ComponentModel.DataAnnotations;

namespace NGA.Models
{
    public class Black : BaseEntity
    {
        [MaxLength(500)]
        public string Title { get; set; }
    }
}
