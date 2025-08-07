using Repositories.DTOs;
using Repositories.Interfaces;
using Repositories.Models;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderStatusHistoryRepository _statusHistoryRepository;
        private readonly IDepositRepository _depositRepository;
        private readonly IReturnInspectionRepository _returnInspectionRepository;
        private readonly ICostumeRepository _costumeRepository;

        public OrderService(
            IOrderRepository orderRepository,
            IOrderStatusHistoryRepository statusHistoryRepository,
            IDepositRepository depositRepository,
            IReturnInspectionRepository returnInspectionRepository,
            ICostumeRepository costumeRepository)
        {
            _orderRepository = orderRepository;
            _statusHistoryRepository = statusHistoryRepository;
            _depositRepository = depositRepository;
            _returnInspectionRepository = returnInspectionRepository;
            _costumeRepository = costumeRepository;
        }

        public async Task<IEnumerable<OrderSummaryDto>> GetAllOrdersAsync()
        {
            var orders = await _orderRepository.GetAllAsync();

            return orders.Select(order => new OrderSummaryDto
            {
                OrderId = order.OrderId,
                Status = order.Status,
                TotalPrice = order.TotalPrice,
                RentStart = order.RentStart,
                RentEnd = order.RentEnd,
                IsPaid = order.IsPaid ?? false,
                CustomerId = order.CustomerId,
                ShippingAddress = order.ShippingAddress,
                RecipientName = order.RecipientName,
                RecipientPhone = order.RecipientPhone,               
            });
        }

        public async Task<Order?> GetOrderByIdAsync(int id) =>
            await _orderRepository.GetByIdAsync(id);

        public async Task<Order> CreateOrderAsync(OrderDto dto)
        {
            if (dto.RentStart.HasValue && dto.RentEnd.HasValue && dto.RentEnd <= dto.RentStart)
                throw new Exception("RentEnd must be after RentStart");

            var order = new Order
            {
                CustomerId = dto.CustomerId,
                Status = dto.Status.ToLower(),
                RentStart = dto.RentStart,
                RentEnd = dto.RentEnd,
                IsPaid = dto.IsPaid,
                Note = dto.Note,
                RecipientName = dto.RecipientName,
                RecipientPhone = dto.RecipientPhone,
                ShippingAddress = dto.ShippingAddress,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var items = new List<OrderItem>();
            foreach (var itemDto in dto.Items)
            {
                var costume = await _costumeRepository.GetByIdAsync(itemDto.CostumeId);
                if (costume == null) throw new Exception("Costume không tồn tại");

                var price = itemDto.IsRental ? costume.PriceRent : costume.PriceSale;
                if (price == null) throw new Exception("Không có giá phù hợp cho costume này");

                items.Add(new OrderItem
                {
                    CostumeId = itemDto.CostumeId,
                    Quantity = itemDto.Quantity,
                    Price = price.Value,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }

            order.OrderItems = items;
            order.TotalPrice = items.Sum(x => x.Quantity * x.Price);

            var createdOrder = await _orderRepository.CreateAsync(order);

            await _statusHistoryRepository.CreateAsync(new OrderStatusHistory
            {
                OrderId = createdOrder.OrderId,
                Status = createdOrder.Status,
                ChangedAt = DateTime.Now,
                Note = "Order created"
            });

            // ✅ Chỉ tính tiền cọc theo yêu cầu
            decimal totalDeposit = 0;
            int totalQuantity = 0;

            foreach (var item in items)
            {
                var costume = await _costumeRepository.GetByIdAsync(item.CostumeId);
                if (costume == null) throw new Exception("Costume không tồn tại khi tính cọc");

                var priceSale = costume.PriceSale ?? throw new Exception("Không có giá bán");

                totalDeposit += priceSale * item.Quantity;
                totalQuantity += item.Quantity;
            }

            if (totalQuantity > 10)
            {
                totalDeposit /= 2;
            }

            await _depositRepository.CreateAsync(new Deposit
            {
                OrderId = createdOrder.OrderId,
                DepositAmount = decimal.Round(totalDeposit, 0),
                DepositStatus = "pending",
                PaymentMethod = dto.PaymentMethod ?? "cash",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });

            return createdOrder;
        }

        public async Task UpdateOrderAsync(int id, OrderDto dto)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null) throw new Exception("Order not found");

            if (dto.RentStart.HasValue && dto.RentEnd.HasValue && dto.RentEnd <= dto.RentStart)
                throw new Exception("RentEnd must be after RentStart");

            order.Status = dto.Status.ToLower();
            order.RentStart = dto.RentStart;
            order.RentEnd = dto.RentEnd;
            order.IsPaid = dto.IsPaid;
            order.Note = dto.Note;
            order.RecipientName = dto.RecipientName;
            order.RecipientPhone = dto.RecipientPhone;
            order.ShippingAddress = dto.ShippingAddress;
            order.UpdatedAt = DateTime.Now;

            await _orderRepository.UpdateAsync(order);
        }

        public async Task DeleteOrderAsync(int id)
        {
            await _orderRepository.DeleteAsync(id);
        }

        public async Task UpdateOrderStatusAsync(int id, string status)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null) throw new Exception("Order not found");

            var validStatuses = new[] {
                "pending", "confirmed", "shipped",
                "returned", "completed", "cancelled"
            };

            if (!validStatuses.Contains(status.ToLower()))
                throw new Exception("Invalid status");

            order.Status = status.ToLower();
            order.UpdatedAt = DateTime.Now;

            await _orderRepository.UpdateAsync(order);

            await _statusHistoryRepository.CreateAsync(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                Status = order.Status,
                ChangedAt = DateTime.Now,
                Note = "Status updated manually"
            });
        }

        public async Task RecordReturnInspectionAsync(ReturnInspectionDto dto)
        {
            var order = await _orderRepository.GetByIdAsync(dto.OrderId);
            if (order == null) throw new Exception("Order not found");

            var existing = await _returnInspectionRepository.GetByOrderIdAsync(dto.OrderId);
            if (existing != null) throw new Exception("Return inspection already recorded");

            await _returnInspectionRepository.CreateAsync(new ReturnInspection
            {
                OrderId = dto.OrderId,
                Condition = dto.Condition,
                PenaltyAmount = dto.PenaltyAmount,
                Note = dto.Note,
                CheckedAt = DateTime.Now
            });

            order.Status = "returned";
            order.UpdatedAt = DateTime.Now;
            await _orderRepository.UpdateAsync(order);

            await _statusHistoryRepository.CreateAsync(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                Status = "returned",
                ChangedAt = DateTime.Now,
                Note = "Return inspection completed"
            });
        }

        public async Task<OrderDetailDto> GetOrderDetailAsync(int id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null) throw new Exception("Order not found");

            var dto = new OrderDetailDto
            {
                OrderId = order.OrderId,
                Status = order.Status,
                RentStart = order.RentStart,
                RentEnd = order.RentEnd,
                TotalPrice = order.TotalPrice,
                IsPaid = order.IsPaid ?? false,
                Note = order.Note,
                RecipientName = order.RecipientName,
                RecipientPhone = order.RecipientPhone,
                ShippingAddress = order.ShippingAddress,
                Items = order.OrderItems.Select(x => new OrderItemDto
                {
                    CostumeId = x.CostumeId,
                    Quantity = x.Quantity,
                    IsRental = order.RentStart != null // tạm dựa vào logic ngày thuê
                }).ToList(),
                Deposit = order.Deposit != null ? new DepositDto
                {
                    OrderId = order.Deposit.OrderId,
                    DepositAmount = order.Deposit.DepositAmount,
                    PaymentMethod = order.Deposit.PaymentMethod,
                    DepositStatus = order.Deposit.DepositStatus
                } : null,
                ReturnInfo = order.ReturnInspection != null ? new ReturnInspectionDto
                {
                    OrderId = order.ReturnInspection.OrderId,
                    Condition = order.ReturnInspection.Condition,
                    PenaltyAmount = order.ReturnInspection.PenaltyAmount,
                    Note = order.ReturnInspection.Note
                } : null,
                StatusHistories = order.StatusHistories?.Select(h => new OrderStatusHistoryDto
                {
                    OrderId = h.OrderId,
                    Status = h.Status,
                    Note = h.Note
                }).ToList() ?? new List<OrderStatusHistoryDto>()
            };

            return dto;
        }
        public async Task UpdateDepositStatusAsync(int orderId, string newStatus)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null) throw new Exception("Order not found");

            var deposit = order.Deposit;
            if (deposit == null) throw new Exception("Deposit not found for this order");

            var validStatuses = new[] { "pending", "paid", "refunded", "cancelled" };
            if (!validStatuses.Contains(newStatus.ToLower()))
                throw new Exception("Invalid deposit status");

            deposit.DepositStatus = newStatus.ToLower();
            deposit.UpdatedAt = DateTime.Now;

            // ✅ Nếu đã thanh toán, thì đánh dấu đơn hàng là IsPaid = true
            if (deposit.DepositStatus == "paid")
            {
                order.IsPaid = true;
                order.UpdatedAt = DateTime.Now;
                await _orderRepository.UpdateAsync(order);
            }

            await _depositRepository.UpdateAsync(deposit);
        }


    }
}
