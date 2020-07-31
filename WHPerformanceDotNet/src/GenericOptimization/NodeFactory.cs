using System;
using System.Collections.Generic;
using System.Text;

namespace GenericOptimization
{
    public class NodeFactory
    {
        /// <summary>
        /// 泛型约束:new() 实际上会调用 Activator.CreateIntance(obj)
        /// 有性能损失
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <returns></returns>
        public static TNode CreateNode<TNode>()
            where TNode : class, new()
        {
            return new TNode();
        }
    }
}
