using System;
using System.Collections.Generic;
using Xunit;

namespace Tote.BasicWallet
{
    namespace Domain
    {
        public interface INotification
        {
            
        }

        public class FundsDeposited : INotification
        {
            public FundsDeposited(decimal amount)
            {
                Amount = amount;
            }
            
            public decimal Amount { get; private set; }
        }
        
        public class FundsWithdrawn : INotification
        {
            public FundsWithdrawn(decimal amount)
            {
                Amount = amount;
            }
            
            public decimal Amount { get; private set; }
        }
        
        public class FundsSpent : INotification
        {
            public FundsSpent(decimal amount)
            {
                Amount = amount;
            }
            
            public decimal Amount { get; private set; }
        }

        public class FundsWithdrawalFailed : INotification
        {
            public FundsWithdrawalFailed(decimal amount, decimal balance, string reason)
            {
                Amount = amount;
                Balance = balance;
                Reason = reason;
            }
            
            public decimal Amount { get; private set; }
            public decimal Balance { get; private set; }
            public string Reason { get; private set; }
        }
        
        public class FundsSpendFailed : INotification
        {
            public FundsSpendFailed(decimal amount, decimal balance, string reason)
            {
                Amount = amount;
                Balance = balance;
                Reason = reason;
            }
            
            public decimal Amount { get; private set; }
            public decimal Balance { get; private set; }
            public string Reason { get; private set; }
        }

        public class BasicWallet
        {
            public BasicWallet(IEnumerable<INotification> notifications = null)
            {
                LoadFromNotifications(notifications);
            }
            
            public decimal Balance { get; private set; }
            
            public INotification DepositFunds(decimal amount)
            {
                if (amount <= 0)
                    throw new Exception("You must deposit more than zero");
                
                return ApplyNotification(new FundsDeposited(amount));
            }

            public INotification WithdrawFunds(decimal amount)
            {
                if (amount <= 0)
                    throw new Exception("You must withdraw more than zero");

                if (Balance < amount)
                    return ApplyNotification(new FundsWithdrawalFailed(amount, Balance, "There are insufficient funds for the withdrawal"));

                return ApplyNotification(new FundsWithdrawn(amount));
            }

            public INotification SpendFunds(decimal amount)
            {
                if (amount <= 0)
                    throw new Exception("You must spend more than zero");
                
                if (Balance < amount)
                    return ApplyNotification(new FundsSpendFailed(amount, Balance, "There are insufficient funds for the spend"));

                return ApplyNotification(new FundsSpent(amount));
            }

            private FundsDeposited ApplyNotification(FundsDeposited fundsDeposited)
            {
                Balance += fundsDeposited.Amount;
                return fundsDeposited;
            }

            private FundsWithdrawn ApplyNotification(FundsWithdrawn fundsWithdrawn)
            {
                Balance -= fundsWithdrawn.Amount;
                return fundsWithdrawn;
            }
            
            private FundsSpent ApplyNotification(FundsSpent fundsSpent)
            {
                Balance -= fundsSpent.Amount;
                return fundsSpent;
            }

            private FundsWithdrawalFailed ApplyNotification(FundsWithdrawalFailed fundsWithdrawalFailed)
            {
                // NOOP
                return fundsWithdrawalFailed;
            }
            
            private FundsSpendFailed ApplyNotification(FundsSpendFailed fundsSpendFailed)
            {
                // NOOP
                return fundsSpendFailed;
            }

            private void LoadFromNotifications(IEnumerable<INotification> notifications)
            {
                if (notifications == null)
                    return;

                foreach (var notification in notifications)
                    ((dynamic) this).ApplyNotification((dynamic)notification);
            }
        }
        
        public class BasicWalletTests
        {
            [Fact]
            public void BalanceMustBeSumOfDeposits()
            {
                var wallet = new BasicWallet();
                const decimal deposit1Amount = 10;
                const decimal deposit2Amount = 9.99m;

                var notification1 = Assert.IsType<FundsDeposited>(wallet.DepositFunds(deposit1Amount));
                Assert.Equal(deposit1Amount, notification1.Amount);

                var notification2 = Assert.IsType<FundsDeposited>(wallet.DepositFunds(deposit2Amount));
                Assert.Equal(deposit2Amount, notification2.Amount);
                
                Assert.Equal(deposit1Amount + deposit2Amount, wallet.Balance);
            }
            
            [Fact]
            public void MustNotifyOfSpendFailureIfBalanceInsufficient()
            {
                var wallet = new BasicWallet();

                const decimal amount = 100;

                var notification = wallet.SpendFunds(amount);

                var fundsSpendFailedNotification = Assert.IsType<FundsSpendFailed>(notification);
                
                Assert.Equal(amount, fundsSpendFailedNotification.Amount);
                Assert.Equal(0m, fundsSpendFailedNotification.Balance);
            }

            [Fact]
            public void SpendMustDecreaseBalanceBySpendAmount()
            {
                var wallet = new BasicWallet();
                const decimal depositAmount = 100;
                const decimal spendAmount = 4.99m;

                wallet.DepositFunds(depositAmount);

                var notification = Assert.IsType<FundsSpent>(wallet.SpendFunds(4.99m));
                Assert.Equal(spendAmount, notification.Amount);
                
                Assert.Equal(depositAmount - spendAmount, wallet.Balance);
            }

            [Theory]
            [InlineData(0)]
            [InlineData(-1)]
            public void AmountsMustNotBeZeroOrLess(decimal value)
            {
                var wallet = new BasicWallet();

                Assert.Throws<Exception>(() => wallet.WithdrawFunds(value));
                Assert.Throws<Exception>(() => wallet.SpendFunds(value));
            }

            [Fact]
            public void LoadingNotificationsMustRestoreState()
            {
                var wallet = new BasicWallet(new INotification[]
                {
                    new FundsDeposited(10),
                    new FundsWithdrawn(5),
                    new FundsSpent(2.5m)
                });

                Assert.Equal(2.5m, wallet.Balance);
            }
        }
    }
}
