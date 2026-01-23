using JfYu.Data.Model;
using NGA.Models.Constant;
using System;
using System.ComponentModel.DataAnnotations;

namespace NGA.Api.Model
{

    public class WeChatUser : BaseEntity
    {
        [MaxLength(100)]
        public string? SessionKey { get; set; }

        [MaxLength(100)]
        public string? OpenId { get; set; }

        [MaxLength(100)]
        public string? UnionId { get; set; }

        [MaxLength(50)]
        public string? NickName { get; set; }

        [MaxLength(50)]
        public string? RealName { get; set; }

        [MaxLength(500)]
        public string? Avatar { get; set; }
                 
        [MaxLength(10)]
        public GenderEnum? Gender { get; set; }
         
        [MaxLength(50)]
        public string? Province { get; set; }
         
        [MaxLength(50)]
        public string? City { get; set; }
         
        [MaxLength(50)]
        public string? Country { get; set; }
         
        [MaxLength(500)]
        public string? Address { get; set; }
         
        [MaxLength(50)]
        public string? Phone { get; set; }          
        public UserRoleEnum Role { get; set; } = UserRoleEnum.General;
        public DateTime LastLoginTime { get; set; } = DateTime.Now;       

    }
}
