using jfYu.Core.MongoDB;

namespace Ngb.Api.Models
{
    public class Black : BaseEntity
    {

        private string title;

        public string Title { get => title; set => title = value; }
    }
}
