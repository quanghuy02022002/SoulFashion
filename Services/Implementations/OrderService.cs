using Repositories.DTOs;
using Repositories.Interfaces;
using Repositories.Models;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderStatusHistoryRepository _statusHistoryRepository;
        private readonly IDepositRepository _depositRepository;
        private readonly IReturnInspectionRepository _returnInspectionRepository;

        public OrderService(
            IOrderRepository orderRepository,
            IOrderStatusHistoryRepository statusHistoryRepository,
            IDepositRepository depositRepository,
            IReturnInspectionRepository returnInspectionRepository)
        {
            _orderRepository = orderRepository;
            _statusHistoryRepository = statusHistoryRepository;
            _depositRepository = depositRepository;
            _returnInspectionRepository = returnInspectionRepository;
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync() =>
            await _orderRepository.GetAllAsync();

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
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,

                // 👉 Thêm danh sách sản phẩm
                OrderItems = dto.Items.Select(x => new OrderItem
                {
                    CostumeId = x.CostumeId,
                    Quantity = x.Quantity,
                    Price = x.Price,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }).ToList()
            };

            // 👉 Tính tổng tiền dựa vào item
            order.TotalPrice = order.OrderItems.Sum(x => x.Quantity * x.Price);

            var createdOrder = await _orderRepository.CreateAsync(order);

            await _statusHistoryRepository.CreateAsync(new OrderStatusHistory
            {
                OrderId = createdOrder.OrderId,
                Status = createdOrder.Status,
                ChangedAt = DateTime.Now,
                Note = "Order created"
            });

            // 👉 Nếu là đơn thuê thì tính tiền cọc
            //if (createdOrder.RentStart.HasValue && createdOrder.RentEnd.HasValue)
            //{
            //    var rentalDays = (createdOrder.RentEnd.Value - createdOrder.RentStart.Value).Days;
            //    var baseDeposit = createdOrder.TotalPrice ?? 0m;
            //    var suggestedDeposit = Math.Max(
            //        baseDeposit * 0.5m,
            //        baseDeposit / rentalDays * 3m);

            //    await _depositRepository.CreateAsync(new Deposit
            //    {
            //        OrderId = createdOrder.OrderId,
            //        DepositAmount = decimal.Round(suggestedDeposit, 0),
            //        DepositStatus = "pending",
            //        PaymentMethod = dto.PaymentMethod ?? "cash, zalopay, vnpay",
            //        CreatedAt = DateTime.Now,
            //        UpdatedAt = DateTime.Now
            //    });
            //}

            return createdOrder;
        }


        public async Task UpdateOrderAsync(int id, OrderDto dto)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null) throw new Exception("Order not found");

            if (dto.RentStart.HasValue && dto.RentEnd.HasValue && dto.RentEnd <= dto.RentStart)
                throw new Exception("RentEnd must be after RentStart");

            order.Status = dto.Status.ToLower();
            order.TotalPrice = dto.TotalPrice;
            order.RentStart = dto.RentStart;
            order.RentEnd = dto.RentEnd;
            order.IsPaid = dto.IsPaid;
            order.Note = dto.Note;
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
                Items = order.OrderItems.Select(x => new OrderItemDto
                {
                    CostumeId = x.CostumeId,
                    Quantity = x.Quantity,
                    Price = x.Price
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
    }
}
