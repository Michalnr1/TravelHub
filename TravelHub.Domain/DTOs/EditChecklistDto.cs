using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelHub.Domain.DTOs
{
    public class EditChecklistDto
    {
        public int TripId { get; set; }
        public string OldItem { get; set; } = "";
        public string NewItem { get; set; } = "";
    }
}
