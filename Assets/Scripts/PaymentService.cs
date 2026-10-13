using System;

namespace Breakout
{
    /// <summary>订单状态：覆盖「下单 → 支付回调 → 发货」以及掉单/重复回调等异常分支。</summary>
    public enum OrderStatus
    {
        None = 0,
        Created = 1,
        Paid = 2,
        Delivered = 3,
        Failed = 4,
        Refunded = 5
    }

    /// <summary>支付回调结果，供 UI 层决定提示文案。</summary>
    public enum CallbackResult
    {
        Success = 0,
        DuplicateCallback = 1,
        OrderNotFound = 2,
        AmountMismatch = 3,
        OrderNotPayable = 4
    }

    /// <summary>一笔订单（本地模拟的收银台单据）。</summary>
    public class Order
    {
        public string OrderId { get; }
        public int Amount { get; }
        public OrderStatus Status { get; internal set; }
        public string ItemId { get; internal set; }
        public int DeliverCount { get; internal set; }

        public Order(string orderId, int amount, string itemId)
        {
            if (string.IsNullOrEmpty(orderId)) throw new ArgumentException("orderId 不能为空", nameof(orderId));
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            OrderId = orderId;
            Amount = amount;
            ItemId = itemId;
            Status = OrderStatus.Created;
        }
    }

    /// <summary>
    /// 纯 C# 的「账号/支付」业务逻辑模拟（不下单到真实渠道）。
    ///
    /// 支付逻辑的风险几乎都在异常分支上，所以这里刻意把
    /// 重复回调（幂等）、金额不符、订单不存在、订单状态不可支付
    /// 都建模成显式返回值，让每条分支都能被单元测试覆盖。
    /// </summary>
    public class PaymentService
    {
        private readonly System.Collections.Generic.Dictionary<string, Order> orders =
            new System.Collections.Generic.Dictionary<string, Order>();

        /// <summary>创建订单（下单）。同一订单号重复创建会被拒绝。</summary>
        public Order CreateOrder(string orderId, int amount, string itemId)
        {
            if (orders.ContainsKey(orderId)) return null;
            var order = new Order(orderId, amount, itemId);
            orders[orderId] = order;
            return order;
        }

        public Order GetOrder(string orderId)
            => orders.TryGetValue(orderId, out var order) ? order : null;

        /// <summary>支付渠道回调（带金额校验 + 幂等）。</summary>
        public CallbackResult HandleCallback(string orderId, int paidAmount, bool channelSuccess)
        {
            var order = GetOrder(orderId);
            if (order == null) return CallbackResult.OrderNotFound;

            // 幂等：已经处理过的回调（渠道会重试）直接返回重复，不再发货
            if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Delivered)
                return CallbackResult.DuplicateCallback;

            if (order.Status != OrderStatus.Created)
                return CallbackResult.OrderNotPayable;

            if (!channelSuccess)
            {
                order.Status = OrderStatus.Failed;
                return CallbackResult.Success;
            }

            if (paidAmount != order.Amount)
                return CallbackResult.AmountMismatch;

            order.Status = OrderStatus.Paid;
            return CallbackResult.Success;
        }

        /// <summary>发货（只有已支付且未发货的订单能发货）。</summary>
        public bool Deliver(string orderId)
        {
            var order = GetOrder(orderId);
            if (order == null || order.Status != OrderStatus.Paid) return false;
            order.Status = OrderStatus.Delivered;
            order.DeliverCount++;
            return true;
        }

        /// <summary>退款（已支付/已发货可退，重复退款返回 false）。</summary>
        public bool Refund(string orderId)
        {
            var order = GetOrder(orderId);
            if (order == null) return false;
            if (order.Status != OrderStatus.Paid && order.Status != OrderStatus.Delivered) return false;
            order.Status = OrderStatus.Refunded;
            return true;
        }
    }
}
