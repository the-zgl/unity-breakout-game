// 无需 Unity 编辑器即可运行的逻辑自检程序。
//
// 用途：把游戏里的纯 C# 逻辑（GameState / PaymentService）拉到标准 .NET 上跑一遍，
// 做到「clone 下来就能验证代码是对的」，也方便在 CI 里给出秒级反馈。
// 完整断言在 Unity Test Framework 的 Assets/Tests 里，两边覆盖同一套业务规则。
//
// 运行： dotnet run --project tools/logic-smoke

using System;
using System.Collections.Generic;

namespace Breakout.Tools
{
    internal static class Program
    {
        private static int passed;
        private static readonly List<string> failures = new List<string>();

        private static void Check(bool condition, string name)
        {
            if (condition)
            {
                passed++;
                Console.WriteLine("  PASS  " + name);
            }
            else
            {
                failures.Add(name);
                Console.WriteLine("  FAIL  " + name);
            }
        }

        private static void Throws<T>(Action action, string name) where T : Exception
        {
            try
            {
                action();
                Check(false, name + "（期望抛 " + typeof(T).Name + "，实际没有抛）");
            }
            catch (T)
            {
                Check(true, name);
            }
            catch (Exception ex)
            {
                Check(false, name + "（抛了 " + ex.GetType().Name + "）");
            }
        }

        private static int Main()
        {
            Console.WriteLine("== GameState：计分 / 生命 / 金币 / 模拟购买 ==");

            var s = new GameState();
            s.AddScore(100);
            Check(s.Score == 100 && s.Coins == 10, "AddScore(100) → 100 分、10 金币");

            s.AddScore(0);
            s.AddScore(-5);
            Check(s.Score == 100, "非正数加分被忽略");

            var edge = new GameState();
            edge.AddScore(9);
            Check(edge.Coins == 0, "9 分不给金币（不足 10）");
            edge.AddScore(1);
            Check(edge.Coins == 1, "累计到 10 分给 1 金币（曾经的 bug）");

            var carry = new GameState();
            carry.AddScore(95);   // 9 金币 + 5 分零头
            carry.Reset();
            carry.AddScore(5);
            Check(carry.Coins == 0, "Reset 后零头不跨局累计");

            var best = new GameState();
            best.AddScore(300);
            best.Reset();
            best.AddScore(100);
            Check(best.BestScore == 300, "Reset 不清除历史最高分");

            var life = new GameState(3);
            Check(life.LoseLife() && life.Lives == 2, "失去一条命后还剩 2 条");
            Check(!new GameState(1).LoseLife(), "最后一条命失去后游戏结束");

            var dead = new GameState(0);
            dead.LoseLife();
            Check(dead.Lives == 0, "生命不会变成负数");

            var buy = new GameState();
            buy.AddScore(200); // 20 金币
            Check(buy.TryPurchase(20) && buy.Coins == 0, "刚好够钱可以购买");
            Check(!buy.TryPurchase(1), "金币不足时购买失败");
            Check(!buy.TryPurchase(-1), "负数价格被拒绝（防刷金币）");
            Check(buy.Coins == 0, "购买失败时金币不变");

            var reset = new GameState();
            reset.AddScore(500);
            reset.LoseLife();
            reset.Reset(3);
            Check(reset.Score == 0 && reset.Coins == 0 && reset.Lives == 3, "Reset 重置分数/金币/生命");

            Console.WriteLine();
            Console.WriteLine("== PaymentService：下单 / 回调 / 发货 / 退款 ==");

            var pay = new PaymentService();
            var order = pay.CreateOrder("ORDER-1", 600, "item_prop_1");
            Check(order != null && order.Status == OrderStatus.Created, "下单成功，状态为已创建");
            Check(pay.CreateOrder("ORDER-1", 600, "item_prop_1") == null, "重复订单号被拒绝");

            Check(pay.HandleCallback("ORDER-1", 600, true) == CallbackResult.Success, "正常回调成功");
            Check(pay.GetOrder("ORDER-1").Status == OrderStatus.Paid, "回调后订单为已支付");
            Check(pay.HandleCallback("ORDER-1", 600, true) == CallbackResult.DuplicateCallback,
                "渠道重复回调是幂等的");

            Check(pay.Deliver("ORDER-1"), "已支付订单可以发货");
            Check(!pay.Deliver("ORDER-1"), "重复发货被拒绝");
            Check(pay.GetOrder("ORDER-1").DeliverCount == 1, "只发货一次");

            pay.HandleCallback("ORDER-1", 600, true);
            Check(pay.GetOrder("ORDER-1").DeliverCount == 1, "重复回调不会重复发货");

            Check(pay.HandleCallback("NOT-EXIST", 600, true) == CallbackResult.OrderNotFound,
                "订单不存在被识别");

            var mismatch = new PaymentService();
            mismatch.CreateOrder("ORDER-2", 600, "item");
            Check(mismatch.HandleCallback("ORDER-2", 599, true) == CallbackResult.AmountMismatch,
                "金额不符被拦截");
            Check(mismatch.GetOrder("ORDER-2").Status == OrderStatus.Created,
                "金额不符时订单仍是未支付");

            var failed = new PaymentService();
            failed.CreateOrder("ORDER-3", 600, "item");
            failed.HandleCallback("ORDER-3", 600, false);
            Check(failed.GetOrder("ORDER-3").Status == OrderStatus.Failed, "渠道失败订单置为失败");
            Check(!failed.Deliver("ORDER-3"), "支付失败不能发货");
            Check(failed.HandleCallback("ORDER-3", 600, true) == CallbackResult.OrderNotPayable,
                "已关闭订单不能直接支付成功");

            var refund = new PaymentService();
            refund.CreateOrder("ORDER-4", 600, "item");
            refund.HandleCallback("ORDER-4", 600, true);
            Check(refund.Refund("ORDER-4"), "已支付订单可以退款");
            Check(!refund.Refund("ORDER-4"), "重复退款被拒绝");

            var unpaid = new PaymentService();
            unpaid.CreateOrder("ORDER-5", 600, "item");
            Check(!unpaid.Refund("ORDER-5"), "未支付订单不能退款");

            Throws<ArgumentOutOfRangeException>(
                () => new PaymentService().CreateOrder("ORDER-6", 0, "item"), "金额为 0 抛参数异常");
            Throws<ArgumentException>(
                () => new PaymentService().CreateOrder("", 600, "item"), "订单号为空抛参数异常");

            Console.WriteLine();
            Console.WriteLine($"通过 {passed} 项，失败 {failures.Count} 项。");
            if (failures.Count > 0)
            {
                Console.WriteLine("失败清单：");
                foreach (var f in failures) Console.WriteLine("  - " + f);
                return 1;
            }

            Console.WriteLine("逻辑自检全部通过 ✅");
            return 0;
        }
    }
}
