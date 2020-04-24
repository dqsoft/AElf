using AElf.Contracts.Configuration;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Economic;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Profit;
using AElf.Contracts.TestContract.MethodCallThreshold;
using AElf.Contracts.TestContract.TransactionFeeCharging;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.TokenHolder;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;
using AElf.Cryptography.ECDSA;
using Volo.Abp.Threading;

namespace AElf.Contracts.EconomicSystem.Tests
{
    // ReSharper disable InconsistentNaming
    public class EconomicSystemTestBase : EconomicContractsTestBase
    {
        protected void InitializeContracts()
        {
            DeployAllContracts();
            
            AsyncHelper.RunSync(InitializeParliamentContract);
            AsyncHelper.RunSync(InitializeTreasuryConverter);
            AsyncHelper.RunSync(InitializeElection);
            AsyncHelper.RunSync(InitializeEconomicContract);
            AsyncHelper.RunSync(InitializeToken);
            AsyncHelper.RunSync(InitializeAElfConsensus);
            AsyncHelper.RunSync(InitializeTokenConverter);
            AsyncHelper.RunSync(InitializeTransactionFeeChargingContract);
        }
        
        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub =>
            GetBasicContractTester(BootMinerKeyPair);

        internal TokenContractImplContainer.TokenContractImplStub TokenContractStub => GetTokenContractTester(BootMinerKeyPair);
        
        internal TokenContractImplContainer.TokenContractImplStub TokenContractImplStub =>
            GetTokenContractImplTester(BootMinerKeyPair);

        internal TokenHolderContractContainer.TokenHolderContractStub TokenHolderStub =>
            GetTokenHolderTester(BootMinerKeyPair);
        
        internal TokenConverterContractContainer.TokenConverterContractStub TokenConverterContractStub =>
            GetTokenConverterContractTester(BootMinerKeyPair);

        internal VoteContractContainer.VoteContractStub VoteContractStub => GetVoteContractTester(BootMinerKeyPair);

        internal ProfitContractContainer.ProfitContractStub ProfitContractStub =>
            GetProfitContractTester(BootMinerKeyPair);

        internal ElectionContractContainer.ElectionContractStub ElectionContractStub =>
            GetElectionContractTester(BootMinerKeyPair);

        internal AEDPoSContractContainer.AEDPoSContractStub AEDPoSContractStub =>
            GetAEDPoSContractTester(BootMinerKeyPair);

        internal AEDPoSContractImplContainer.AEDPoSContractImplStub AedPoSContractImplStub =>
            GetAEDPoSImplContractTester(BootMinerKeyPair);

        internal TreasuryContractContainer.TreasuryContractStub TreasuryContractStub =>
            GetTreasuryContractTester(BootMinerKeyPair);

        internal ParliamentContractContainer.ParliamentContractStub ParliamentContractStub =>
            GetParliamentContractTester(BootMinerKeyPair);

        internal TransactionFeeChargingContractContainer.TransactionFeeChargingContractStub
            TransactionFeeChargingContractStub => GetTransactionFeeChargingContractTester(BootMinerKeyPair);

        internal MethodCallThresholdContractContainer.MethodCallThresholdContractStub MethodCallThresholdContractStub =>
            GetMethodCallThresholdContractTester(BootMinerKeyPair);

        internal EconomicContractContainer.EconomicContractStub EconomicContractStub =>
            GetEconomicContractTester(BootMinerKeyPair);

        internal ConfigurationContainer.ConfigurationStub ConfigurationContractStub =>
            GetConfigurationContractTester(BootMinerKeyPair);
        
        internal BasicContractZeroContainer.BasicContractZeroStub GetBasicContractTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }

        internal TokenContractImplContainer.TokenContractImplStub GetTokenContractTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, keyPair);
        }
        
        internal TokenContractImplContainer.TokenContractImplStub GetTokenContractImplTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, keyPair);
        }

        internal TokenHolderContractContainer.TokenHolderContractStub GetTokenHolderTester(ECKeyPair keyPair)
        {
            return GetTester<TokenHolderContractContainer.TokenHolderContractStub>(TokenHolderContractAddress, keyPair);
        }
        
        internal TokenConverterContractContainer.TokenConverterContractStub GetTokenConverterContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterContractAddress,
                keyPair);
        }

        internal VoteContractContainer.VoteContractStub GetVoteContractTester(ECKeyPair keyPair)
        {
            return GetTester<VoteContractContainer.VoteContractStub>(VoteContractAddress, keyPair);
        }

        internal ProfitContractContainer.ProfitContractStub GetProfitContractTester(ECKeyPair keyPair)
        {
            return GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, keyPair);
        }

        internal ElectionContractContainer.ElectionContractStub GetElectionContractTester(ECKeyPair keyPair)
        {
            return GetTester<ElectionContractContainer.ElectionContractStub>(ElectionContractAddress, keyPair);
        }

        internal AEDPoSContractContainer.AEDPoSContractStub GetAEDPoSContractTester(ECKeyPair keyPair)
        {
            return GetTester<AEDPoSContractContainer.AEDPoSContractStub>(ConsensusContractAddress, keyPair);
        }

        internal AEDPoSContractImplContainer.AEDPoSContractImplStub GetAEDPoSImplContractTester(ECKeyPair keyPair)
        {
            return GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(ConsensusContractAddress, keyPair);
        }

        internal TreasuryContractContainer.TreasuryContractStub GetTreasuryContractTester(ECKeyPair keyPair)
        {
            return GetTester<TreasuryContractContainer.TreasuryContractStub>(TreasuryContractAddress, keyPair);
        }

        internal ParliamentContractContainer.ParliamentContractStub GetParliamentContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<ParliamentContractContainer.ParliamentContractStub>(ParliamentContractAddress,
                keyPair);
        }

        internal TransactionFeeChargingContractContainer.TransactionFeeChargingContractStub
            GetTransactionFeeChargingContractTester(ECKeyPair keyPair)
        {
            return GetTester<TransactionFeeChargingContractContainer.TransactionFeeChargingContractStub>(
                TransactionFeeChargingContractAddress, keyPair);
        }

        internal MethodCallThresholdContractContainer.MethodCallThresholdContractStub
            GetMethodCallThresholdContractTester(
                ECKeyPair keyPair)
        {
            return GetTester<MethodCallThresholdContractContainer.MethodCallThresholdContractStub>(
                MethodCallThresholdContractAddress,
                keyPair);
        }

        internal EconomicContractContainer.EconomicContractStub GetEconomicContractTester(ECKeyPair keyPair)
        {
            return GetTester<EconomicContractContainer.EconomicContractStub>(EconomicContractAddress, keyPair);
        }
        
        internal ConfigurationContainer.ConfigurationStub GetConfigurationContractTester(ECKeyPair keyPair)
        {
            return GetTester<ConfigurationContainer.ConfigurationStub>(ConfigurationAddress, keyPair);
        }
    }
}