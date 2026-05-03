namespace Application.Common.Interfaces
{
    public interface IOrderEventPublisher
    {
        Task PublishOrderCreatedAsync(int tenantId, int userId, decimal amount, string description, string customerEmail);
        Task PublishOrderUpdatedAsync(int orderId, int tenantId, string status);
    }
}
