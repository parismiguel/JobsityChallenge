using System;
using System.ComponentModel.DataAnnotations;

namespace Chat.Web.Models
{
    public class AuditBase
    {
        public AuditBase()
        {
            CreatedOn = DateTime.Now;
            CreatedBy = "System";
        }

        [Display(Name = "Creado por")]
        public string CreatedBy { get; set; }

        [Display(Name = "Actualizado por")]
        public string UpdatedBy { get; set; }

        [Display(Name = "Fecha creado")]
        public DateTime CreatedOn { get; set; }

        [Display(Name = "Fecha creado")]
        public string CreatedOnFormatted { get { return CreatedOn.ToString("yyyy-MM-dd HH:mm:ss"); } }

        [Display(Name = "Fecha actualizado")]
        public DateTime UpdatedOn { get; set; }

        [Display(Name = "Fecha Actualizado")]
        public string UpdatedOnFormatted { get { return UpdatedOn.ToString("yyyy-MM-dd HH:mm:ss"); } }

    }
}
