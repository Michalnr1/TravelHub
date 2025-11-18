using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelHub.Domain.Entities
{
    public class ChecklistItem
    {
        public string Title { get; set; } = "";

        public bool IsCompleted { get; set; } = false;

        public int? AssignedParticipantId { get; set; }

        public string? AssignedParticipantName { get; set; }
    }

}
