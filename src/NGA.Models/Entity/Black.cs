using JfYu.Data.Model;
using System.ComponentModel.DataAnnotations;

namespace NGA.Models.Entity
{
    public class Black : BaseEntity
    {
        [MaxLength(500)]
        public string Title { get; set; }
    }
}
