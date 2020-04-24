using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Kernel.Txn.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    /// <summary>
    /// Return true if native token balance of from address is greater than 0.
    /// </summary>
    internal class MethodFeeAffordableValidationProvider : ITransactionValidationProvider
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub>
            _contractReaderFactory;
        private readonly IPrimaryTokenSymbolProvider _primaryTokenSymbolProvider;
        private readonly ITransactionFeeExemptionService _feeExemptionService;

        public ILogger<MethodFeeAffordableValidationProvider> Logger { get; set; }

        public MethodFeeAffordableValidationProvider(IBlockchainService blockchainService,
            IPrimaryTokenSymbolProvider primaryTokenSymbolProvider,
            ITransactionFeeExemptionService feeExemptionService,
            ISmartContractAddressService smartContractAddressService,
            IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub> contractReaderFactory)
        {
            _blockchainService = blockchainService;
            _primaryTokenSymbolProvider = primaryTokenSymbolProvider;
            _feeExemptionService = feeExemptionService;
            _smartContractAddressService = smartContractAddressService;
            _contractReaderFactory = contractReaderFactory;
        }

        public bool ValidateWhileSyncing => false;

        public async Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            // Skip if this is a system transaction.
            if (_feeExemptionService.IsFree(transaction))
            {
                return true;
            }

            var chain = await _blockchainService.GetChainAsync();

            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };

            // Skip this validation at the very beginning of current chain.
            if (chain.LastIrreversibleBlockHeight == AElfConstants.GenesisBlockHeight)
            {
                return true;
            }

            var tokenContractAddress =
                _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);
            var tokenStub = _contractReaderFactory.Create(new ContractReaderContext
            {
                BlockHash = chainContext.BlockHash,
                BlockHeight = chainContext.BlockHeight,
                ContractAddress = tokenContractAddress
            });
            var balance = (await tokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = transaction.From,
                Symbol = await _primaryTokenSymbolProvider.GetPrimaryTokenSymbol()
            }))?.Balance;
            // balance == null means token contract hasn't deployed.
            if (balance == null || balance > 0) return true;

            Logger.LogWarning($"Empty balance of tx from address: {transaction}");
            return false;
        }
    }
}