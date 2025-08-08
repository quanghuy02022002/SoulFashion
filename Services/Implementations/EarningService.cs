using Microsoft.EntityFrameworkCore;
using Repositories.Implementations;
using Repositories.Interfaces;
using Repositories.Models;
using Services.Interfaces;

public class EarningService : IEarningService
{
    private readonly AppDBContext _ctx;
    private readonly ICollaboratorEearningRepository _earningRepo;

    private const decimal ADMIN_RATE = 0.30m;
    private int? _adminUserIdCache; // cache trong vòng đời service

    public EarningService(AppDBContext ctx, ICollaboratorEearningRepository earningRepo)
    {
        _ctx = ctx;
        _earningRepo = earningRepo;
    }

    private async Task<int> GetAdminUserIdAsync()
    {
        if (_adminUserIdCache.HasValue) return _adminUserIdCache.Value;

        var adminIds = await _ctx.Users
            .Where(u => u.Role == "admin" && u.IsActive == true)
            .Select(u => u.UserId)
            .ToListAsync();

        if (adminIds.Count() == 0)
            throw new Exception("Không tìm thấy tài khoản admin.");
        if (adminIds.Count() > 1)
            throw new Exception("Có hơn 1 tài khoản admin.");

        _adminUserIdCache = adminIds[0];
        return _adminUserIdCache.Value;
    }

    public async Task RebuildEarningsForOrderAsync(int orderId)
    {
        // Idempotent: xoá earnings cũ của order rồi tạo lại
        await _earningRepo.DeleteByOrderIdAsync(orderId);

        var adminUserId = await GetAdminUserIdAsync();

        var order = await _ctx.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Costume)
            .FirstOrDefaultAsync(o => o.OrderId == orderId)
            ?? throw new Exception("Order not found");

        var isRentalOrder = order.RentStart != null; // đơn thuê
        var now = DateTime.Now;
        var toInsert = new List<CollaboratorEarning>();

        foreach (var item in order.OrderItems)
        {
            var priceRent = item.Costume?.PriceRent;
            var isRentalItem = isRentalOrder || (priceRent.HasValue && priceRent.Value == item.Price);
            if (!isRentalItem) continue; // chỉ tính tiền thuê, bỏ bán & bỏ cọc

            var rentalRevenue = item.Price * item.Quantity; // chỉ tiền thuê
            if (rentalRevenue <= 0) continue;

            var adminAmount = decimal.Round(rentalRevenue * ADMIN_RATE, 0);
            var collabAmount = rentalRevenue - adminAmount; // tránh lệch do round

            var collaboratorUserId = item.Costume?.CreatedByUserId; // người tạo Costume là collaborator

            if (collaboratorUserId.HasValue && collaboratorUserId.Value != adminUserId)
            {
                // 70% cho collaborator
                toInsert.Add(new CollaboratorEarning
                {
                    UserId = collaboratorUserId.Value,
                    OrderItemId = item.OrderItemId,
                    EarningAmount = collabAmount,
                    Status = "pending",
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            else
            {
                // không có collaborator (hoặc chính admin tạo) → admin nhận 100%
                adminAmount = rentalRevenue;
            }

            // 30% cho admin (hoặc 100% nếu không có collaborator)
            toInsert.Add(new CollaboratorEarning
            {
                UserId = adminUserId,
                OrderItemId = item.OrderItemId,
                EarningAmount = adminAmount,
                Status = "pending",
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (toInsert.Count > 0)
            await _earningRepo.AddRangeAsync(toInsert);
    }
}