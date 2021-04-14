using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpGuide.generics
{
    // 使用泛型继承
    public interface IPolicyValidator
    {
        bool Validate(IPolicy policy);
    }

    public abstract class PolicyValidator<TPolicy> :
        IPolicyValidator, IPolicyValidator<TPolicy> where TPolicy : IPolicy
    {
        public bool Validate(IPolicy policy) => Validate((TPolicy)policy);

        public abstract bool Validate(TPolicy policy);
    }

    public class LifePolicyValidator2 : PolicyValidator<LifePolicy>
    {
        public override bool Validate(LifePolicy policy)
        {
            return policy.PolicyName == "Life";
        }
    }
    public class HomePolicyValidator2 : PolicyValidator<HomePolicy>
    {
        public override bool Validate(HomePolicy policy)
        {
            return policy.PolicyName == "Home";
        }
    }
    public class AutoPolicyValidator2 : PolicyValidator<AutoPolicy>
    {
        public override bool Validate(AutoPolicy policy)
        {
            return policy.PolicyName == "Auto";
        }
    }
}
