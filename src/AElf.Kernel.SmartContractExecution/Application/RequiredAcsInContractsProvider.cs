using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.CSharp.CodeOps.Validators.Assembly;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface IRequiredAcsInContractsProvider
    {
        Task<RequiredAcsDto> GetRequiredAcsInContractsAsync(Hash blockHash, long blockHeight);
    }

    public class RequiredAcsInContractsProvider : IRequiredAcsInContractsProvider, ISingletonDependency
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        
        private Address ConfigurationContractAddress => _smartContractAddressService.GetAddressByContractName(
            ConfigurationSmartContractAddressNameProvider.Name);
        
        //TODO: strange way
        private Address FromAddress { get; } = Address.FromBytes(new byte[] { }.ComputeHash());

        public RequiredAcsInContractsProvider(ISmartContractAddressService smartContractAddressService, 
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService)
        {
            _smartContractAddressService = smartContractAddressService;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
        }

        public async Task<RequiredAcsDto> GetRequiredAcsInContractsAsync(Hash blockHash, long blockHeight)
        {
            var tx = new Transaction
            {
                From = FromAddress,
                To = ConfigurationContractAddress,
                MethodName = nameof(ConfigurationContainer.ConfigurationStub.GetRequiredAcsInContracts),
                Params = new Empty().ToByteString(),
                Signature = ByteString.CopyFromUtf8(KernelConstants.SignaturePlaceholder)
            };

            var returned = await _transactionReadOnlyExecutionService.ExecuteAsync<RequiredAcsInContracts>(
                new ChainContext
                {
                    BlockHash = blockHash,
                    BlockHeight = blockHeight
                }, tx, TimestampHelper.GetUtcNow(), false);

            return new RequiredAcsDto
            {
                AcsList = returned.AcsList.ToList(),
                RequireAll = returned.RequireAll
            };
        }
    }
}