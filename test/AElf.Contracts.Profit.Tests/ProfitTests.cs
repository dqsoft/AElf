using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Profit
{
    public partial class ProfitTests : ProfitContractTestBase
    {
        public ProfitTests()
        {
            InitializeContracts();
        }

        [Fact]
        public async Task ProfitContract_CheckTreasury()
        {
            await CreateTreasury();
        }

        /// <summary>
        /// Of course it's okay for an address to creator many profit items.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ProfitContract_CreateManyProfitItems()
        {
            const int createTimes = 5;

            var creator = Creators[0];
            var creatorAddress = Address.FromPublicKey(CreatorMinerKeyPair[0].PublicKey);

            for (var i = 0; i < createTimes; i++)
            {
                var executionResult = await creator.CreateScheme.SendAsync(new CreateSchemeInput
                {
                });
                executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            var createdSchemeIds = await creator.GetCreatedSchemeIds.CallAsync(new GetCreatedSchemeIdsInput
            {
                Creator = creatorAddress
            });

            createdSchemeIds.SchemeIds.Count.ShouldBe(createTimes);
        }

        [Fact]
        public async Task ProfitContract_DistributeProfits()
        {
            const int amount = 1000;

            var creator = Creators[0];

            var schemeId = await CreateScheme();

            // Add profits to virtual address of this profit item.
            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Symbol = ProfitContractTestConsts.NativeTokenSymbol,
            });

            // Check profit item and corresponding balance.
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                Assert.Equal(amount, profitItem.UndistributedProfits[ProfitContractTestConsts.NativeTokenSymbol]);

                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = profitItem.VirtualAddress,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(amount);
            }

            // Add profits to release profits virtual address of this profit item.
            const int period = 3;
            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Period = period,
                Symbol = ProfitContractTestConsts.NativeTokenSymbol,
            });

            // Check profit item and corresponding balance.
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                // Total amount stay.
                profitItem.UndistributedProfits[ProfitContractTestConsts.NativeTokenSymbol].ShouldBe(amount);

                var virtualAddress = await creator.GetSchemeAddress.CallAsync(
                    new SchemePeriod
                    {
                        SchemeId = schemeId,
                        Period = period
                    });
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = virtualAddress,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(amount);

                var releasedProfitInformation = await creator.GetDistributedProfitsInfo.CallAsync(
                    new SchemePeriod
                    {
                        SchemeId = schemeId,
                        Period = period
                    });
                releasedProfitInformation.IsReleased.ShouldBe(false);
                releasedProfitInformation.TotalShares.ShouldBe(0);
                releasedProfitInformation.ProfitsAmount[ProfitContractTestConsts.NativeTokenSymbol].ShouldBe(amount);
            }
        }

        /// <summary>
        /// It's valid for a third party account to add profits to any profit item.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ProfitContract_DistributeProfits_ByThirdParty()
        {
            const long amountReleasedByCreator = 1000;
            const long amountAddedByGoodGuy = 1000;
            const long shares = 100;

            var creator = Creators[0];
            var goodGuy = Creators[1];

            var schemeId = await CreateScheme();

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput()
            {
                SchemeId = schemeId,
                BeneficiaryShare = new BeneficiaryShare {Beneficiary = Address.Generate(), Shares = shares}
            });

            // Add profits to virtual address of this profit item.
            await goodGuy.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amountAddedByGoodGuy,
                Symbol = ProfitContractTestConsts.NativeTokenSymbol,
            });

            // Check profit item.
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                profitItem.UndistributedProfits[ProfitContractTestConsts.NativeTokenSymbol].ShouldBe(amountReleasedByCreator);
            }
            
            // Add profits to release profits virtual address of this profit item.
            await goodGuy.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amountAddedByGoodGuy,
                Symbol = ProfitContractTestConsts.NativeTokenSymbol,
                Period = 1
            });

            // Check balance of period 1
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                // Total amount stay.
                profitItem.UndistributedProfits[ProfitContractTestConsts.NativeTokenSymbol].ShouldBe(amountReleasedByCreator);

                var releasedProfitsInformation = await creator.GetDistributedProfitsInfo.CallAsync(
                    new SchemePeriod
                    {
                        SchemeId = schemeId,
                        Period = 1
                    });
                releasedProfitsInformation.ProfitsAmount[ProfitContractTestConsts.NativeTokenSymbol].ShouldBe(amountReleasedByCreator);
                // total_Shares is 0 before releasing.
                releasedProfitsInformation.TotalShares.ShouldBe(0);
                releasedProfitsInformation.IsReleased.ShouldBe(false);

                var virtualAddress = await creator.GetSchemeAddress.CallAsync(
                    new SchemePeriod
                    {
                        SchemeId = schemeId,
                        Period = 1
                    });
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = virtualAddress,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(amountReleasedByCreator);
            }

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Period = 1,
                Amount = amountReleasedByCreator
            });

            // Creator can release profits of this period.
            {
                var releasedProfitsInformation = await creator.GetDistributedProfitsInfo.CallAsync(
                    new SchemePeriod
                    {
                        SchemeId = schemeId,
                        Period = 1
                    });
                releasedProfitsInformation.ProfitsAmount[ProfitContractTestConsts.NativeTokenSymbol]
                    .ShouldBe(amountReleasedByCreator + amountAddedByGoodGuy);
                releasedProfitsInformation.TotalShares.ShouldBe(shares);
                releasedProfitsInformation.IsReleased.ShouldBe(true);
            }
        }

        [Fact]
        public async Task ProfitContract_RemoveSubScheme()
        {
            const int shares1 = 80;
            const int shares2 = 20;

            var creator = Creators[0];

            var schemeId = await CreateScheme();
            var subSchemeId1 = await CreateScheme(1);
            var subSchemeId2 = await CreateScheme(2);

            var subProfitItem1 = await creator.GetScheme.CallAsync(subSchemeId1);
            var subProfitItem2 = await creator.GetScheme.CallAsync(subSchemeId2);

            await creator.AddSubScheme.SendAsync(new AddSubSchemeInput
            {
                SchemeId = schemeId,
                SubSchemeId = subSchemeId1,
                SubSchemeShares = shares1
            });

            var profitDetails1 = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = schemeId,
                Beneficiary = subProfitItem1.VirtualAddress
            });

            // Check the total_weight of profit item.
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                profitItem.TotalShares.ShouldBe(shares1);
            }

            profitDetails1.Details.Count.ShouldBe(1);
            profitDetails1.Details.First().StartPeriod.ShouldBe(1);
            profitDetails1.Details.First().EndPeriod.ShouldBe(long.MaxValue);
            profitDetails1.Details.First().LastProfitPeriod.ShouldBe(0);
            profitDetails1.Details.First().Shares.ShouldBe(shares1);

            await creator.AddSubScheme.SendAsync(new AddSubSchemeInput()
            {
                SchemeId = schemeId,
                SubSchemeId = subSchemeId2,
                SubSchemeShares = shares2
            });

            var profitDetails2 = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = schemeId,
                Beneficiary = subProfitItem2.VirtualAddress
            });

            // Check the total_weight of profit item.
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                profitItem.TotalShares.ShouldBe(shares1 + shares2);
            }

            profitDetails2.Details.Count.ShouldBe(1);
            profitDetails2.Details.First().StartPeriod.ShouldBe(1);
            profitDetails2.Details.First().EndPeriod.ShouldBe(long.MaxValue);
            profitDetails2.Details.First().LastProfitPeriod.ShouldBe(0);
            profitDetails2.Details.First().Shares.ShouldBe(shares2);
        }

        [Fact]
        public async Task ProfitContract_AddWeight()
        {
            var creator = Creators[0];

            var schemeId = await CreateScheme();

            const int shares1 = 100;
            const int shares2 = 200;
            var receiver1 = Address.Generate();
            var receiver2 = Address.Generate();

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = new BeneficiaryShare{Beneficiary = receiver1, Shares = shares1},
                SchemeId = schemeId,
            });

            // Check total_weight and profit_detail
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                profitItem.TotalShares.ShouldBe(shares1);

                var profitDetails = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    SchemeId = schemeId,
                    Beneficiary = receiver1
                });
                profitDetails.Details.Count.ShouldBe(1);
                profitDetails.Details[0].Shares.ShouldBe(shares1);
                profitDetails.Details[0].EndPeriod.ShouldBe(long.MaxValue);
                profitDetails.Details[0].StartPeriod.ShouldBe(1);
                profitDetails.Details[0].LastProfitPeriod.ShouldBe(0);
            }

            const int endPeriod = 100;
            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = {Beneficiary = receiver2, Shares = shares2},
                SchemeId = schemeId,
                EndPeriod = endPeriod
            });

            // Check total_weight and profit_detail
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                profitItem.TotalShares.ShouldBe(shares1 + shares2);

                var profitDetails = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    SchemeId = schemeId,
                    Beneficiary = receiver2
                });
                profitDetails.Details.Count.ShouldBe(1);
                profitDetails.Details[0].Shares.ShouldBe(shares2);
                profitDetails.Details[0].EndPeriod.ShouldBe(endPeriod);
                profitDetails.Details[0].StartPeriod.ShouldBe(1);
                profitDetails.Details[0].LastProfitPeriod.ShouldBe(0);
            }
        }

        [Fact]
        public async Task ProfitContract_AddWeight_IncorrectEndPeriod()
        {
            const long amount = 100;
            const long shares = 10;

            var creator = Creators[0];
            var beneficiary = Address.Generate();

            var schemeId = await CreateScheme();

            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                profitItem.CurrentPeriod.ShouldBe(1);
            }

            await TransferToProfitItemVirtualAddress(schemeId);

            // Current period: 1
            {
                var executionResult = await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
                {
                    SchemeId = schemeId,
                    BeneficiaryShare = {Beneficiary = beneficiary, Shares = shares},
                    EndPeriod = 1
                });

                executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            // We need to add Shares successfully for further testing.
            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                SchemeId = schemeId,
                BeneficiaryShare = {Beneficiary = beneficiary, Shares = shares},
            });

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Period = 1
            });

            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                profitItem.CurrentPeriod.ShouldBe(2);
            }

            // Current period: 2
            {
                var executionResult = await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
                {
                    SchemeId = schemeId,
                    BeneficiaryShare = {Beneficiary = beneficiary, Shares = shares},
                    EndPeriod = 1
                });

                executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                executionResult.TransactionResult.Error.ShouldContain("Invalid end period.");
            }
        }

        [Fact]
        public async Task ProfitContract_AddWeight_ProfitItemNotFound()
        {
            var creator = Creators[0];

            var executionResult = await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                SchemeId = Hash.Generate(),
                BeneficiaryShare = {Beneficiary = Address.Generate(), Shares = 100},
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Profit item not found.");
        }

        /// <summary>
        /// Every time adding Shares to a Beneficiary, will remove expired and used up profit details.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ProfitContract_AddWeight_RemoveExpiredProfitDetails()
        {
            const long expiredPeriodNumber = 1;
            const long amount = 150;
            const long shares = 10;

            var creator = Creators[0];
            var beneficiary = Creators[1];
            
            var receiverAddress = Address.FromPublicKey(CreatorMinerKeyPair[1].PublicKey);

            var schemeId = await CreateScheme(0, expiredPeriodNumber);

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                SchemeId = schemeId,
                BeneficiaryShare = {Beneficiary = receiverAddress, Shares = shares},
                EndPeriod = 1
            });

            await TransferToProfitItemVirtualAddress(schemeId, amount);

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput()
            {
                SchemeId = schemeId,
                Amount = amount / 3,
                Period = 1
            });

            // Check details
            {
                var profitDetails = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    SchemeId = schemeId,
                    Beneficiary = receiverAddress
                });

                profitDetails.Details.Count.ShouldBe(1);
                profitDetails.Details[0].Shares.ShouldBe(shares);
                profitDetails.Details[0].StartPeriod.ShouldBe(1);
                profitDetails.Details[0].EndPeriod.ShouldBe(1);
                profitDetails.Details[0].LastProfitPeriod.ShouldBe(0);
            }

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount / 3,
                Period = 2
            });

            // Check details
            {
                var profitDetails = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    SchemeId = schemeId,
                    Beneficiary = receiverAddress
                });

                profitDetails.Details.Count.ShouldBe(1);
                profitDetails.Details[0].Shares.ShouldBe(shares);
                profitDetails.Details[0].StartPeriod.ShouldBe(1);
                profitDetails.Details[0].EndPeriod.ShouldBe(1);
                profitDetails.Details[0].LastProfitPeriod.ShouldBe(0);
            }

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount / 3,
                Period = 3
            });

            await beneficiary.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeId = schemeId
            });

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                SchemeId = schemeId,
                BeneficiaryShare = {Beneficiary = receiverAddress, Shares = shares * 2},
                EndPeriod = 4
            });

            // Check details
            {
                var profitDetails = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    SchemeId = schemeId,
                    Beneficiary = receiverAddress
                });

                profitDetails.Details.Count.ShouldBe(1);
                profitDetails.Details[0].Shares.ShouldBe(shares * 2);
                profitDetails.Details[0].StartPeriod.ShouldBe(4);
                profitDetails.Details[0].EndPeriod.ShouldBe(4);
                profitDetails.Details[0].LastProfitPeriod.ShouldBe(0);
            }
        }

        [Fact]
        public async Task ProfitContract_SubWeight()
        {
            const int shares = 100;
            const int amount = 100;

            var creator = Creators[0];
            var beneficiary = Normal[0];
            var receiverAddress = Address.FromPublicKey(NormalMinerKeyPair[0].PublicKey);

            var schemeId = await CreateScheme();

            await TransferToProfitItemVirtualAddress(schemeId);

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = {Beneficiary = receiverAddress, Shares = shares},
                SchemeId = schemeId,
                EndPeriod = 1
            });

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Period = 1
            });
            
            // Check total_weight and profit_detail
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                profitItem.TotalShares.ShouldBe(shares);

                var profitDetails = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    SchemeId = schemeId,
                    Beneficiary = receiverAddress
                });
                profitDetails.Details.Count.ShouldBe(1);
            }

            await beneficiary.ClaimProfits.SendAsync(new ClaimProfitsInput {SchemeId = schemeId});
            
            await creator.RemoveBeneficiary.SendAsync(new RemoveBeneficiaryInput
            {
                Beneficiary = receiverAddress,
                SchemeId = schemeId
            });

            // Check total_weight and profit_detail
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                profitItem.TotalShares.ShouldBe(0);

                var profitDetails = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    SchemeId = schemeId,
                    Beneficiary = receiverAddress
                });
                profitDetails.Details.Count.ShouldBe(0);
            }
        }

        [Fact]
        public async Task ProfitContract_SubWeight_ProfitItemNotFound()
        {
            var creator = Creators[0];

            var executionResult = await creator.RemoveBeneficiary.SendAsync(new RemoveBeneficiaryInput
            {
                SchemeId = Hash.Generate(),
                Beneficiary = Address.Generate()
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Profit item not found.");
        }

        [Fact]
        public async Task ProfitContract_ReleaseProfits_WithoutEnoughBalance()
        {
            const long amount = 100;

            var creator = Creators[0];

            var schemeId = await CreateScheme();

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = {Beneficiary = Address.Generate(), Shares = 100},
                SchemeId = schemeId,
            });

            var executionResult = await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Period = 1
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Insufficient profits amount.");
        }

        [Fact]
        public async Task ProfitContract_ReleaseProfits_InvalidPeriod()
        {
            const long amount = 100;

            var creator = Creators[0];

            var schemeId = await CreateScheme();

            await TransferToProfitItemVirtualAddress(schemeId);

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = {Beneficiary = Address.Generate(), Shares = 100},
                SchemeId = schemeId,
            });

            var executionResult = await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Period = 2
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Invalid period.");
        }

        [Fact]
        public async Task ProfitContract_ReleaseProfits_NotCreator()
        {
            const long amount = 100;

            var schemeId = await CreateScheme();

            await TransferToProfitItemVirtualAddress(schemeId);

            // The actual creator is Creators[0]
            var anotherGuy = Creators[1];

            var executionResult = await anotherGuy.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Period = 1
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Only creator can release profits.");
        }

        [Fact]
        public async Task ProfitContract_ReleaseProfits_ProfitItemNotFound()
        {
            const long amount = 100;

            var user = Creators[0];

            var executionResult = await user.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = Hash.Generate(),
                Amount = amount,
                Period = 1
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Profit item not found.");
        }

        [Fact]
        public async Task ProfitContract_ReleaseProfits()
        {
            const long amount = 100;

            var creator = Creators[0];

            var schemeId = await CreateScheme();

            await TransferToProfitItemVirtualAddress(schemeId, amount * 2);

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = {Beneficiary = Address.Generate(), Shares = 100},
                SchemeId = schemeId,
            });

            // First time.
            {
                var executionResult = await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
                {
                    SchemeId = schemeId,
                    Amount = amount,
                    Period = 1
                });

                executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            // Second time.
            {
                var executionResult = await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
                {
                    SchemeId = schemeId,
                    Amount = amount,
                    Period = 2
                });

                executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [Fact]
        public async Task ProfitContract_ReleaseProfits_BurnProfits()
        {
            const long amount = 100;
            // Will burn specific amount of profits. Because no one can receive profits from -1 period.
            const long period = -1;

            var creator = Creators[0];

            var schemeId = await CreateScheme();

            await TransferToProfitItemVirtualAddress(schemeId, amount);
            
            var executionResult = await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Period = period
            });
            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            // Check balance.
            var address = await ProfitContractStub.GetSchemeAddress.CallAsync(new SchemePeriod
                {SchemeId = schemeId, Period = -1});
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {Owner = address, Symbol = ProfitContractTestConsts.NativeTokenSymbol});
            balance.Balance.ShouldBe(amount);
        }

        [Fact]
        public async Task ProfitContract_ReleaseProfits_WithSubProfitItems()
        {
            const long amount = 100;
            const long weight1 = 80;
            const long weight2 = 20;

            var creator = Creators[0];

            var schemeId = await CreateScheme();
            var subSchemeId1 = await CreateScheme(1);
            var subSchemeId2 = await CreateScheme(2);

            await TransferToProfitItemVirtualAddress(schemeId);

            // Check balance of main profit item.
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = profitItem.VirtualAddress,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;

                balance.ShouldBe(amount);
            }

            await creator.AddSubScheme.SendAsync(new AddSubSchemeInput
            {
                SchemeId = schemeId,
                SubSchemeId = subSchemeId1,
                SubSchemeShares = weight1
            });

            await creator.AddSubScheme.SendAsync(new AddSubSchemeInput
            {
                SchemeId = schemeId,
                SubSchemeId = subSchemeId2,
                SubSchemeShares = weight2
            });

            var executionResult = await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Period = 1
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            // Check balance of first sub profit item.
            {
                var subProfitItem = await creator.GetScheme.CallAsync(subSchemeId1);
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = subProfitItem.VirtualAddress,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;

                balance.ShouldBe(amount.Mul(weight1).Div(weight1 + weight2));
            }

            // Check balance of second sub profit item.
            {
                var subProfitItem = await creator.GetScheme.CallAsync(subSchemeId2);
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = subProfitItem.VirtualAddress,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;

                balance.ShouldBe(amount.Mul(weight2).Div(weight1 + weight2));
            }
        }

        [Fact]
        public async Task ProfitContract_Profit()
        {
            const long shares = 100;
            const long amount = 100;

            var creator = Creators[0];
            var beneficiary = Normal[0];
            var receiverAddress = Address.FromPublicKey(NormalMinerKeyPair[0].PublicKey);

            var schemeId = await CreateScheme();

            await TransferToProfitItemVirtualAddress(schemeId);

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = {Beneficiary = receiverAddress, Shares = shares},
                SchemeId = schemeId,
            });

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Period = 1
            });

            await beneficiary.ClaimProfits.SendAsync(new ClaimProfitsInput {SchemeId = schemeId});

            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = receiverAddress,
                Symbol = ProfitContractTestConsts.NativeTokenSymbol
            })).Balance;
            balance.ShouldBe(amount);
        }
        
        [Fact]
        public async Task ProfitContract_Profit_TwoReceivers()
        {
            const long weight1 = 100;
            const long weight2 = 400;
            const long amount = 100;

            var creator = Creators[0];
            var receiver1 = Normal[0];
            var receiver2 = Normal[1];
            var receiverAddress1 = Address.FromPublicKey(NormalMinerKeyPair[0].PublicKey);
            var receiverAddress2 = Address.FromPublicKey(NormalMinerKeyPair[1].PublicKey);

            var schemeId = await CreateScheme();

            await TransferToProfitItemVirtualAddress(schemeId);

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = {Beneficiary = receiverAddress1, Shares = weight1},
                SchemeId = schemeId,
            });
            
            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = {Beneficiary = receiverAddress2, Shares = weight2},
                SchemeId = schemeId,
            });

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Period = 1
            });

            await receiver1.ClaimProfits.SendAsync(new ClaimProfitsInput {SchemeId = schemeId});

            // Check balance of Beneficiary 1.
            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = receiverAddress1,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(weight1.Mul(amount).Div(weight1 + weight2));
            }
            
            await receiver2.ClaimProfits.SendAsync(new ClaimProfitsInput {SchemeId = schemeId});

            // Check balance of Beneficiary 2.
            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = receiverAddress2,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(weight2.Mul(amount).Div(weight1 + weight2));
            }
        }

        [Fact]
        public async Task ProfitContract_Profit_RegisteredSubProfitItems()
        {
            const long weight1 = 100;
            const long weight2 = 400;
            const long weight3 = 500;
            const long amount = 100;

            var creator = Creators[0];
            var receiver1 = Normal[0];
            var receiver2 = Normal[1];
            var receiverAddress1 = Address.FromPublicKey(NormalMinerKeyPair[0].PublicKey);
            var receiverAddress2 = Address.FromPublicKey(NormalMinerKeyPair[1].PublicKey);

            var schemeId = await CreateScheme();
            var subSchemeId = await CreateScheme(1);

            await TransferToProfitItemVirtualAddress(schemeId);

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = {Beneficiary = receiverAddress1, Shares = weight1},
                SchemeId = schemeId,
            });

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = {Beneficiary = receiverAddress2, Shares = weight2},
                SchemeId = schemeId,
            });

            await creator.AddSubScheme.SendAsync(new AddSubSchemeInput
            {
                SchemeId = schemeId,
                SubSchemeId = subSchemeId,
                SubSchemeShares = weight3
            });

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Period = 1
            });

            await receiver1.ClaimProfits.SendAsync(new ClaimProfitsInput {SchemeId = schemeId});

            // Check balance of Beneficiary 1.
            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = receiverAddress1,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(weight1.Mul(amount).Div(weight1.Add(weight2).Add(weight3)));
            }
            
            await receiver2.ClaimProfits.SendAsync(new ClaimProfitsInput {SchemeId = schemeId});

            // Check balance of Beneficiary 2.
            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = receiverAddress2,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(weight2.Mul(amount).Div(weight1.Add(weight2).Add(weight3)));
            }
        }

        [Fact]
        public async Task ProfitContract_Profit_ProfitItemNotFound()
        {
            var beneficiary = Normal[0];

            var executionResult = await beneficiary.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeId = Hash.Generate()
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Profit item not found.");
        }

        [Fact]
        public async Task ProfitContract_Profit_NotRegisteredBefore()
        {
            const long amount = 100;

            var creator = Creators[0];
            var beneficiary = Normal[0];

            var schemeId = await CreateScheme();
            
            await TransferToProfitItemVirtualAddress(schemeId);

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Period = 1
            });
            
            var executionResult = await beneficiary.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeId = schemeId
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Profit details not found.");
        }

        [Fact]
        public async Task ProfitContract_Profit_MultiplePeriods()
        {
            const int periodCount = 5;
            const long shares = 100;
            const long amount = 100;

            var creator = Creators[0];
            var beneficiary = Normal[0];
            var beneficiaryAddress = Address.FromPublicKey(NormalMinerKeyPair[0].PublicKey);

            var schemeId = await CreateScheme();

            await TransferToProfitItemVirtualAddress(schemeId, amount * periodCount + amount);

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                SchemeId = schemeId,
                BeneficiaryShare = {Beneficiary = beneficiaryAddress, Shares = shares},
            });

            for (var i = 0; i < periodCount; i++)
            {
                await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
                {
                    SchemeId = schemeId,
                    Amount = amount,
                    Period = i + 1
                });
            }

            await beneficiary.ClaimProfits.SendAsync(new ClaimProfitsInput {SchemeId = schemeId});

            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = beneficiaryAddress,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(amount * periodCount);

                var details = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    SchemeId = schemeId,
                    Beneficiary = beneficiaryAddress
                });
                details.Details[0].LastProfitPeriod.ShouldBe(periodCount + 1);
            }

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                Period = periodCount + 1,
                Amount = amount,
                SchemeId = schemeId
            });

            await beneficiary.ClaimProfits.SendAsync(new ClaimProfitsInput {SchemeId = schemeId});

            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = beneficiaryAddress,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(amount * periodCount + amount);

                var details = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    SchemeId = schemeId,
                    Beneficiary = beneficiaryAddress
                });
                details.Details[0].LastProfitPeriod.ShouldBe(periodCount + 2);
            }          
        }

        private async Task<Hash> CreateScheme(int returnIndex = 0,
            long profitReceivingDuePeriodCount = ProfitContractConsts.DefaultProfitReceivingDuePeriodCount)
        {
            var creator = Creators[0];
            var creatorAddress = Address.FromPublicKey(CreatorMinerKeyPair[0].PublicKey);

            await creator.CreateScheme.SendAsync(new CreateSchemeInput
            {
                ProfitReceivingDuePeriodCount = profitReceivingDuePeriodCount
            });

            var createdSchemeIds = (await creator.GetCreatedSchemeIds.CallAsync(new GetCreatedSchemeIdsInput
            {
                Creator = creatorAddress
            })).SchemeIds;

            return createdSchemeIds[returnIndex];
        }

        private async Task TransferToProfitItemVirtualAddress(Hash schemeId, long amount = 100)
        {
            await ProfitContractStub.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Symbol = ProfitContractTestConsts.NativeTokenSymbol,
                Amount = amount
            });
        }
    }
}