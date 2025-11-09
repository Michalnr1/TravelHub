using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelHub.Domain.DTOs
{
    public class ChatMessageCreateDto
    {
        /// <summary>
        /// The message text.
        /// </summary>
        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Optional PersonId.
        /// </summary>
        public string? PersonId { get; set; }
    }

}
