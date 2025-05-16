using System;
using System.ComponentModel.DataAnnotations;

namespace BaiTHbuoi1.Models
{
    public class asp_mssv
    {
        [Key]
        [Display(Name = "Mã số sinh viên")]
        public string MSSV { get; set; }

        [Display(Name = "Họ tên")]
        public string TENSV { get; set; }

        [Display(Name = "Số điện thoại")]
        public string SODT { get; set; }

        [Display(Name = "Lớp")]
        public string LOP { get; set; }

        [Display(Name = "Địa chỉ")]
        public string DIACHI { get; set; }

        [Display(Name = "Email")]
        public string EMAIL { get; set; }
    }
}
