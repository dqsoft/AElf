using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Types.Tests
{
    public class StateValueTests
    {
        [Fact]
        public void StateValue_Test()
        {
            var hashArray = HashHelper.ComputeFromString("hash").ToByteArray();
            var stateValue = StateValue.Create(hashArray);
            var isDirty = stateValue.IsDirty;
            isDirty.ShouldBeFalse();

            var hashArray1 = stateValue.Get();
            hashArray.ShouldBe(hashArray1);

            var hashArray2 = HashHelper.ComputeFromString("hash1").ToByteArray();
            stateValue.Set(hashArray2);

            isDirty = stateValue.IsDirty;
            isDirty.ShouldBeTrue();

            var hashArray3 = stateValue.Get();
            hashArray3.ShouldBe(hashArray2);
            
            stateValue.OriginalValue.ShouldBe(hashArray);
            stateValue.CurrentValue.ShouldBe(hashArray2);
        }
    }
}