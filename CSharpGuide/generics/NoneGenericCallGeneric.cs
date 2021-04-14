using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

// https://jimmybogard.com/crossing-the-generics-divide/
namespace CSharpGuide.generics
{
    public class NoneGenericCallGeneric
    {
        [AllowNull]
        public IServiceProvider ServiceProvider;
        public List<IPolicy> Policies = new();

        public void Start()
        {
            bool isValid = false;
            foreach (var policy in Policies)
            {
                // container.GetService<IPolicyValidator<??>>();
                // 这里不知道从容器中拿具体的类型
                var policyValidator = ServiceProvider.GetService<IPolicyValidator<IPolicy>>();
                isValid = policyValidator!.Validate(policy);
                // 所以还是得从“硬编码”走模式匹配
                switch (policy)
                {
                    case AutoPolicy auto:
                        var autoPolicyValidator = ServiceProvider.GetService<IPolicyValidator<AutoPolicy>>()!;
                        isValid = isValid && autoPolicyValidator.Validate(auto);
                        break;
                    case HomePolicy home:
                        var homePolicyValidator = ServiceProvider.GetService<IPolicyValidator<HomePolicy>>()!;
                        isValid = isValid && homePolicyValidator.Validate(home);
                        break;
                    case LifePolicy life:
                        var lifePolicyValidator = ServiceProvider.GetService<IPolicyValidator<LifePolicy>>()!;
                        isValid = isValid && lifePolicyValidator.Validate(life);
                        break;
                }
            }

            // 使用泛型继承重构
            foreach (var policy in Policies)
            {
                // 可以手动告诉编译器具体的泛型类型
                var policyType = policy.GetType();
                var validatorType = typeof(IPolicyValidator<>).MakeGenericType(policyType);
                var policyValidator = (IPolicyValidator)ServiceProvider.GetService(validatorType)!;
                isValid = isValid && policyValidator.Validate(policy);
            }

            // 继承容易破坏数据结构，是导致不稳定的主要因素
            // 可以优先使用组合方式
            foreach (var policy in Policies)
            {
                // 可以手动告诉编译器具体的泛型类型
                var policyType = policy.GetType();
                var validatorType = typeof(PolicyValidator2<>).MakeGenericType(policyType);
                var policyValidator = (IPolicyValidator)ServiceProvider.GetService(validatorType)!;
                isValid = isValid && policyValidator.Validate(policy);
            }
        }
    }

    public interface IPolicyValidator<TPolicy>
        where TPolicy : IPolicy
    {
        bool Validate(TPolicy policy);
    }

    // 实现类
    public class LifePolicyValidator : IPolicyValidator<LifePolicy>
    {
        public bool Validate(LifePolicy policy)
        {
            return policy.PolicyName == "Life";
        }
    }
    public class HomePolicyValidator : IPolicyValidator<HomePolicy>
    {
        public bool Validate(HomePolicy policy)
        {
            return policy.PolicyName == "Home";
        }
    }
    public class AutoPolicyValidator : IPolicyValidator<AutoPolicy>
    {
        public bool Validate(AutoPolicy policy)
        {
            return policy.PolicyName == "Auto";
        }
    }
}
