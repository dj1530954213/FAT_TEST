using System.Collections.ObjectModel;
using FatFullVersion.Models;
using FatFullVersion.Services;
using FatFullVersion.IServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fat_UnitTest.Services
{
    [TestClass]
    public class ChannelMappingServiceTests
    {
        private class DummyRepository : IRepository { }

        private readonly IChannelStateManager _stateManager = new ChannelStateManager();

        [TestMethod]
        public void AllocateChannels_Should_Assign_Batch_When_No_PLC_Mappings()
        {
            // Arrange
            var service = new ChannelMappingService(new DummyRepository(), _stateManager);
            var channels = new ObservableCollection<ChannelMapping>
            {
                new ChannelMapping { ModuleType = "AI", VariableName = "PT_1" },
                new ChannelMapping { ModuleType = "DI", VariableName = "DI_1" }
            };

            // Act
            var result = service.AllocateChannelsTestAsync(channels).Result;

            // Assert
            foreach (var ch in result)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(ch.TestBatch), $"{ch.VariableName} 未分配批次");
            }
        }
    }
} 