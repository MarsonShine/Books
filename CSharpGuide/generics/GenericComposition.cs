using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpGuide.generics
{
    // 使用组合方式
    // 泛型桥接类实现非泛型接口，然后转交给内部的泛型方法（类型转换）
    public class PolicyValidator2<TPolicy> :
        IPolicyValidator where TPolicy : IPolicy
    {
        private readonly IPolicyValidator<TPolicy> _inner;
        public PolicyValidator2(IPolicyValidator<TPolicy> inner)
        {
            _inner = inner;
        }

        public bool Validate(IPolicy policy) => _inner.Validate((TPolicy)policy);
    }
}
