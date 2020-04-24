using System.Collections.Generic;
using Acs0;
using AElf.Contracts.Parliament;
using AElf.Kernel.Proposal;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForParliament()
        {
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract(
                GetContractCodeByName("AElf.Contracts.Parliament"),
                ParliamentSmartContractAddressNameProvider.Name,
                GenerateParliamentInitializationCallList());
            return l;
        }
        
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateParliamentInitializationCallList()
        {
            var parliamentInitializationCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            parliamentInitializationCallList.Add(
                nameof(ParliamentContractContainer.ParliamentContractStub.Initialize),
                new Contracts.Parliament.InitializeInput());
            return parliamentInitializationCallList;
        }
    }
}