using System;
using System.Collections.Generic;
using Xunit;

namespace Tote.BasicWalletWithMultiCurrency
{
    namespace Domain
    {
        public interface INotification
        {
            
        }

        public class FundsDeposited : INotification
        {
            public FundsDeposited(decimal amount, CurrencyCode currencyCode)
            {
                Amount = amount;
                CurrencyCode = currencyCode;
            }
            
            public decimal Amount { get; private set; }
            
            public CurrencyCode CurrencyCode { get; private set; }
        }
        
        public class FundsWithdrawn : INotification
        {
            public FundsWithdrawn(decimal amount, CurrencyCode currencyCode)
            {
                Amount = amount;
                CurrencyCode = currencyCode;
            }
            
            public decimal Amount { get; private set; }
            public CurrencyCode CurrencyCode { get; private set; }
        }
        
        public class FundsSpent : INotification
        {
            public FundsSpent(decimal amount, CurrencyCode currencyCode)
            {
                Amount = amount;
            }
            
            public decimal Amount { get; private set; }
            public CurrencyCode CurrencyCode { get; private set; }
        }

        public class FundsWithdrawalFailed : INotification
        {
            public FundsWithdrawalFailed(decimal amount, CurrencyCode currencyCode, decimal balance, string reason)
            {
                Amount = amount;
                CurrencyCode = currencyCode;
                Balance = balance;
                Reason = reason;
            }
            
            public decimal Amount { get; private set; }
            public CurrencyCode CurrencyCode { get; private set; }
            public decimal Balance { get; private set; }
            public string Reason { get; private set; }
        }
        
        public class FundsSpendFailed : INotification
        {
            public FundsSpendFailed(decimal amount, CurrencyCode currencyCode, decimal balance, string reason)
            {
                Amount = amount;
                CurrencyCode = currencyCode;
                Balance = balance;
                Reason = reason;
            }
            
            public decimal Amount { get; private set; }
            public CurrencyCode CurrencyCode { get; private set; }
            public decimal Balance { get; private set; }
            public string Reason { get; private set; }
        }

        public enum CurrencyCode
        {
            GBP,
            EUR
        }

        public class BasicWalletWithMultiCurrency
        {
            private readonly IDictionary<CurrencyCode, decimal> _balances = new Dictionary<CurrencyCode, decimal>();
            
            public BasicWalletWithMultiCurrency(IEnumerable<INotification> notifications = null)
            {
                LoadFromNotifications(notifications);
            }

            public decimal this[CurrencyCode currencyCode] => (_balances.TryGetValue(currencyCode, out var balance) ? balance : 0);

            public INotification DepositFunds(decimal amount, CurrencyCode currencyCode)
            {
                if (amount <= 0)
                    throw new Exception("You must deposit more than zero");
                
                return ApplyNotification(new FundsDeposited(amount, currencyCode));
            }

            public INotification WithdrawFunds(decimal amount, CurrencyCode currencyCode)
            {
                if (amount <= 0)
                    throw new Exception("You must withdraw more than zero");

                var balance = this[currencyCode];

                if (balance < amount)
                    return ApplyNotification(new FundsWithdrawalFailed(amount, currencyCode, balance, "There are insufficient funds for the withdrawal"));

                return ApplyNotification(new FundsWithdrawn(amount, currencyCode));
            }

            public INotification SpendFunds(decimal amount, CurrencyCode currencyCode)
            {
                if (amount <= 0)
                    throw new Exception("You must spend more than zero");
                
                var balance = this[currencyCode];
                
                if (balance < amount)
                    return ApplyNotification(new FundsSpendFailed(amount, currencyCode, balance, "There are insufficient funds for the spend"));

                return ApplyNotification(new FundsSpent(amount, currencyCode));
            }

            private void AdjustBalance(decimal amount, CurrencyCode currencyCode)
            {
                _balances[currencyCode] = this[currencyCode] + amount;
            }
            
            private FundsDeposited ApplyNotification(FundsDeposited fundsDeposited)
            {
                AdjustBalance(fundsDeposited.Amount, fundsDeposited.CurrencyCode);
                return fundsDeposited;
            }

            private FundsWithdrawn ApplyNotification(FundsWithdrawn fundsWithdrawn)
            {
                AdjustBalance(-fundsWithdrawn.Amount, fundsWithdrawn.CurrencyCode);
                return fundsWithdrawn;
            }
            
            private FundsSpent ApplyNotification(FundsSpent fundsSpent)
            {
                AdjustBalance(-fundsSpent.Amount, fundsSpent.CurrencyCode);
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
        
        public class BasicWalletWithCurrencyTests
        {
            [Fact]
            public void DepositsForMultipleCurrenciesMustBeKeptSeparately()
            {
                var wallet = new BasicWalletWithMultiCurrency();
                const decimal gbpDepositAmount = 18.5m;
                const decimal eurDepositAmount = 7.1m;

                wallet.DepositFunds(gbpDepositAmount, CurrencyCode.GBP);
                wallet.DepositFunds(eurDepositAmount, CurrencyCode.EUR);
                
                Assert.Equal(gbpDepositAmount, wallet[CurrencyCode.GBP]);
                Assert.Equal(eurDepositAmount, wallet[CurrencyCode.EUR]);
            }
            
            [Fact]
            public void BalanceMustBeSumOfDeposits()
            {
                var wallet = new BasicWalletWithMultiCurrency();
                const decimal deposit1Amount = 10;
                const decimal deposit2Amount = 9.99m;

                var notification1 = Assert.IsType<FundsDeposited>(wallet.DepositFunds(deposit1Amount, CurrencyCode.GBP));
                Assert.Equal(deposit1Amount, notification1.Amount);

                var notification2 = Assert.IsType<FundsDeposited>(wallet.DepositFunds(deposit2Amount, CurrencyCode.GBP));
                Assert.Equal(deposit2Amount, notification2.Amount);
                
                Assert.Equal(deposit1Amount + deposit2Amount, wallet[CurrencyCode.GBP]);
            }
            
            [Fact]
            public void MustNotifyOfSpendFailureIfBalanceInsufficient()
            {
                var wallet = new BasicWalletWithMultiCurrency();

                const decimal amount = 100;

                var notification = wallet.SpendFunds(amount, CurrencyCode.GBP);

                var fundsSpendFailedNotification = Assert.IsType<FundsSpendFailed>(notification);
                
                Assert.Equal(amount, fundsSpendFailedNotification.Amount);
                Assert.Equal(0m, fundsSpendFailedNotification.Balance);
            }

            [Fact]
            public void SpendMustDecreaseBalanceBySpendAmount()
            {
                var wallet = new BasicWalletWithMultiCurrency();
                const decimal depositAmount = 100;
                const decimal spendAmount = 4.99m;

                wallet.DepositFunds(depositAmount, CurrencyCode.GBP);

                var notification = Assert.IsType<FundsSpent>(wallet.SpendFunds(4.99m, CurrencyCode.GBP));
                Assert.Equal(spendAmount, notification.Amount);
                
                Assert.Equal(depositAmount - spendAmount, wallet[CurrencyCode.GBP]);
            }

            [Theory]
            [InlineData(0)]
            [InlineData(-1)]
            public void AmountsMustNotBeZeroOrLess(decimal value)
            {
                var wallet = new BasicWalletWithMultiCurrency();

                Assert.Throws<Exception>(() => wallet.WithdrawFunds(value, CurrencyCode.GBP));
                Assert.Throws<Exception>(() => wallet.SpendFunds(value, CurrencyCode.GBP));
            }

            [Fact]
            public void LoadingNotificationsMustRestoreState()
            {
                var wallet = new BasicWalletWithMultiCurrency(new INotification[]
                {
                    new FundsDeposited(10, CurrencyCode.GBP),
                    new FundsWithdrawn(5, CurrencyCode.GBP),
                    new FundsSpent(2.5m, CurrencyCode.GBP)
                });

                Assert.Equal(2.5m, wallet[CurrencyCode.GBP]);
            }
        }
    }
}
