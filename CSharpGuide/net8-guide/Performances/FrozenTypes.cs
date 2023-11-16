using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace net8_guide.Performances
{
    /*
     * 这些新引入的 FrozenXXX 集合创建后，就不允许对键和值进行任何更改。这一要求允许更快地进行读取操作。
     */
    internal class FrozenTypes
    {
        private static readonly FrozenDictionary<string, bool> s_configurationData = LoadConfigurationData().ToFrozenDictionary(); // optimizeForReads:true; missing method?
        private static Dictionary<string,bool> LoadConfigurationData()
        {
            throw new NotImplementedException();
        }
    }
}
