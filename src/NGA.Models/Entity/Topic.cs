using JfYu.Data.Model;
using System.ComponentModel.DataAnnotations;

namespace NGA.Models.Entity
{
    public class Topic : BaseEntity
    {
        [MaxLength(500)]
        public string Url { get; set; }

        [MaxLength(20)]
        public string Fid { get; set; }

        [MaxLength(80)]
        public string Uid { get; set; }
        [MaxLength(30)]
        public string Tid { get; set; }
        [MaxLength(200)]
        public string Title { get; set; }
        /// <summary>
        /// 回复数
        /// </summary>
        [MaxLength(200)]
        public string Replies { get; set; }
        [MaxLength(200)]
        public string LastReplyer { get; set; }
        [MaxLength(200)]
        public string PostDate { get; set; }
        [MaxLength(200)]
        public string Thread { get; set; }
        /// <summary>
        /// 0表示主题正常  1正在采集中  -1 错误中断
        /// </summary>
        public int Condition { get; set; }
        /// <summary>
        /// 已抓取数
        /// </summary>
        public int ReptileNum { get; set; }
    }
}
