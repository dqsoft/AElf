using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    public class TokenContractChargeFeeStrategy : ChargeFeeStrategyBase
    {
        public TokenContractChargeFeeStrategy(ISmartContractAddressService smartContractAddressService) : base(
            smartContractAddressService)
        {
        }

        public override string MethodName => string.Empty;

        public override Address ContractAddress =>
            SmartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);

        protected override List<string> GetInvolvedSmartContractMethods()
        {
            return new List<string>
            {
                nameof(TokenContractImplContainer.TokenContractImplStub.DonateResourceToken),

                // Pre-plugin tx
                nameof(TokenContractImplContainer.TokenContractImplStub.CheckResourceToken),

                // Post-plugin tx
                nameof(TokenContractImplContainer.TokenContractImplStub.ChargeResourceToken),
            };
        }
    }
}