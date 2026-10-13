using Breakout;
using NUnit.Framework;

namespace Breakout.Tests
{
    /// <summary>
    /// PaymentService 单元测试：覆盖「账号、支付类业务逻辑」的核心测试要点。
    ///
    /// 支付流程真正的风险不在成功路径，而在异常分支：
    /// 重复回调（渠道重试）、金额不符（篡改/多币种）、订单不存在、已关闭订单再支付。
    /// 这里把每条分支都做成可断言的用例。
    /// </summary>
    public class PaymentServiceTests
    {
        private PaymentService payments;

        [SetUp]
        public void SetUp()
        {
            payments = new PaymentService();
        }

        // ------------------------------------------------------------ 下单

        [Test]
        public void CreateOrder_ReturnsOrderInCreatedState()
        {
            var order = payments.CreateOrder("ORDER-1", 600, "item_prop_1");

            Assert.IsNotNull(order);
            Assert.AreEqual(OrderStatus.Created, order.Status);
            Assert.AreEqual(600, order.Amount);
        }

        [Test]
        public void CreateOrder_DuplicateOrderId_IsRejected()
        {
            payments.CreateOrder("ORDER-1", 600, "item_prop_1");
            var again = payments.CreateOrder("ORDER-1", 600, "item_prop_1");

            Assert.IsNull(again, "同一订单号重复下单必须被拒绝");
        }

        // ------------------------------------------------------ 支付回调正常路径

        [Test]
        public void Callback_ThenDeliver_SucceedsOnce()
        {
            payments.CreateOrder("ORDER-1", 600, "item_prop_1");

            Assert.AreEqual(CallbackResult.Success, payments.HandleCallback("ORDER-1", 600, true));
            Assert.AreEqual(OrderStatus.Paid, payments.GetOrder("ORDER-1").Status);

            Assert.IsTrue(payments.Deliver("ORDER-1"));
            Assert.AreEqual(OrderStatus.Delivered, payments.GetOrder("ORDER-1").Status);
            Assert.AreEqual(1, payments.GetOrder("ORDER-1").DeliverCount);
        }

        // ------------------------------------------------------ 支付回调异常分支

        [Test]
        public void Callback_Repeated_IsIdempotent()
        {
            payments.CreateOrder("ORDER-1", 600, "item_prop_1");
            payments.HandleCallback("ORDER-1", 600, true);

            var second = payments.HandleCallback("ORDER-1", 600, true);

            Assert.AreEqual(CallbackResult.DuplicateCallback, second, "渠道重试时必须幂等");
        }

        [Test]
        public void Callback_Repeated_DoesNotDeliverTwice()
        {
            payments.CreateOrder("ORDER-1", 600, "item_prop_1");
            payments.HandleCallback("ORDER-1", 600, true);
            payments.Deliver("ORDER-1");

            payments.HandleCallback("ORDER-1", 600, true); // 渠道重复通知

            Assert.AreEqual(1, payments.GetOrder("ORDER-1").DeliverCount, "重复回调不能重复发货");
            Assert.IsFalse(payments.Deliver("ORDER-1"), "已发货订单不能再发一次");
        }

        [Test]
        public void Callback_UnknownOrder_ReturnsOrderNotFound()
        {
            Assert.AreEqual(CallbackResult.OrderNotFound, payments.HandleCallback("NOT-EXIST", 600, true));
        }

        [Test]
        public void Callback_AmountMismatch_DoesNotMarkPaid()
        {
            payments.CreateOrder("ORDER-1", 600, "item_prop_1");

            var result = payments.HandleCallback("ORDER-1", 599, true);

            Assert.AreEqual(CallbackResult.AmountMismatch, result);
            Assert.AreEqual(OrderStatus.Created, payments.GetOrder("ORDER-1").Status, "金额不符不能置为已支付");
        }

        [Test]
        public void Callback_ChannelFailure_MarksOrderFailed()
        {
            payments.CreateOrder("ORDER-1", 600, "item_prop_1");

            Assert.AreEqual(CallbackResult.Success, payments.HandleCallback("ORDER-1", 600, false));
            Assert.AreEqual(OrderStatus.Failed, payments.GetOrder("ORDER-1").Status);
            Assert.IsFalse(payments.Deliver("ORDER-1"), "支付失败的订单不能发货");
        }

        [Test]
        public void Callback_FailedOrderRetry_IsNotPayable()
        {
            payments.CreateOrder("ORDER-1", 600, "item_prop_1");
            payments.HandleCallback("ORDER-1", 600, false);

            var retry = payments.HandleCallback("ORDER-1", 600, true);

            Assert.AreEqual(CallbackResult.OrderNotPayable, retry, "已失败关闭的订单不应能直接支付成功");
        }

        // -------------------------------------------------------------- 退款

        [Test]
        public void Refund_PaidOrder_SucceedsOnce()
        {
            payments.CreateOrder("ORDER-1", 600, "item_prop_1");
            payments.HandleCallback("ORDER-1", 600, true);

            Assert.IsTrue(payments.Refund("ORDER-1"));
            Assert.IsFalse(payments.Refund("ORDER-1"), "重复退款必须被拒绝");
            Assert.AreEqual(OrderStatus.Refunded, payments.GetOrder("ORDER-1").Status);
        }

        [Test]
        public void Refund_UnpaidOrder_IsRejected()
        {
            payments.CreateOrder("ORDER-1", 600, "item_prop_1");
            Assert.IsFalse(payments.Refund("ORDER-1"), "未支付订单不能退款");
        }

        // ------------------------------------------------------------ 参数校验

        [Test]
        public void CreateOrder_NonPositiveAmount_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => payments.CreateOrder("ORDER-1", 0, "item"));
        }

        [Test]
        public void CreateOrder_EmptyOrderId_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => payments.CreateOrder("", 600, "item"));
        }
    }
}
