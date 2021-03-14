using System;

namespace TXT.WEAVR.Communication.Entities
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; }

        public Guid CompanyId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
