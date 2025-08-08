namespace Services.Interfaces;

public interface IEarningService
{
    Task RebuildEarningsForOrderAsync(int orderId);
}