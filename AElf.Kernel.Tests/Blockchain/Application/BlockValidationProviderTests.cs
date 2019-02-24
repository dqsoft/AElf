using System.Linq.Expressions;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using Shouldly;
using Xunit;

namespace AElf.Kernel
{
    public class BlockValidationProviderTests : AElfKernelTestBase
    {
        private readonly IBlockValidationProvider _blockValidationProvider;
        
        public BlockValidationProviderTests()
        {
            _blockValidationProvider = GetRequiredService<IBlockValidationProvider>();
        }

        [Fact]
        public async Task Test_Validate_Block_Before_Execute()
        {
            Block block = null;
            bool validateResult;

            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(0, block);
            validateResult.ShouldBeFalse();
            
            block = new Block();
            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(0, block);
            validateResult.ShouldBeFalse();
            
            block.Header = new BlockHeader();
            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(0, block);
            validateResult.ShouldBeFalse();
            
            block.Body = new BlockBody();
            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(0, block);
            validateResult.ShouldBeFalse();
            
            block.Body.Transactions.Add(Hash.Genesis);
            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(0, block);
            validateResult.ShouldBeFalse();

            block.Header.MerkleTreeRootOfTransactions = block.Body.CalculateMerkleTreeRoots();
            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(0, block);
            validateResult.ShouldBeTrue();        
        }

        [Fact]
        public async Task Test_Validate_Block_After_Execute()
        {
            var validateResult = await _blockValidationProvider.ValidateBlockAfterExecuteAsync(0, null);
            validateResult.ShouldBeTrue();
        }
    }
}