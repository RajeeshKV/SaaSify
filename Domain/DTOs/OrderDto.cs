namespace Domain.DTOs
{
    public class OrderDto
    {
        public int OrderId { get; set; }
        public int TenantId { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public class PaginatedOrdersResponse
    {
        public List<OrderDto> Orders { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    // RabbitMQ Event Schemas
    public class OrderCreatedEvent
    {
        public int OrderId { get; set; }
        public int TenantId { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
    }

    public class OrderStatusUpdatedEvent
    {
        public int OrderId { get; set; }
        public int TenantId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}
