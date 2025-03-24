using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FatFullVersion.Entities.EntitiesEnum;

namespace FatFullVersion.Entities.ValueObject
{
    public record ComparisonTable(string ChannelAddress, string CommunicationAddress,TestPlcChannelType ChannelType);
}
