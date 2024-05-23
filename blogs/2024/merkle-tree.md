# Merkle 树

Merkle 树是一种用于验证数据完整性和一致性的树形数据结构，广泛应用于分布式系统、区块链和文件系统中。它的核心思想是通过递归地计算和存储哈希值，从而有效地验证和比较大规模数据集。

## 数据结构

Merkle 数是一种二叉树，其中每个叶子节点包含数据块的哈希值，而每个非叶子节点包含其子节点哈希值的组合(通常是子节点哈希的拼接或哈希)。

树的根节点称为 Merkle 根,包含整个数据集的哈希值。结构图如下

![](../asserts/Hash_Tree.svg)

（该图取自 https://en.wikipedia.org/wiki/Merkle_tree）

## 构建

Merkle 树的构建过程如下:

1. 将数据集分成固定大小的数据块，计算每个数据块的哈希值作为叶子节点。
2. 如果数据块数量不是 2 的幂次方，需要复制最后一个数据块。
3. **从底层开始**，每两个叶子节点的哈希值进行拼接并哈希，生成它们的父节点哈希值。
4. 重复第3步，直到只剩下一个节点，即 Merkle 根。

## 验证

要验证某个数据块是否包含在Merkle树中：

1. 获取目标数据块的哈希值。
2. 从树底层开始，逐级向上计算目标数据块所在路径的哈希值。
3. 将计算得到的哈希值与 Merkle 根进行比较，相同则说明数据块存在于树中。

该验证方法高效,因为只需计算路径上的几个节点哈希值,而不需遍历整个树。

## 示例

假设我们有四个数据块：A、B、 C 和 D。构建 Merkle 树的步骤如下：

1. 计算每个数据块的哈希值：
   - Hash(A)
   - Hash(B)
   - Hash(C)
   - Hash(D)
2. 将相邻的哈希值组合并计算其哈希值：
   - Hash(AB) = Hash(Hash(A) + Hash(B))
   - Hash(CD) = Hash(Hash(C) + Hash(D))
3. 最后，计算根节点的哈希值：
   - Merkle 根 = Hash(Hash(AB) + Hash(CD))

## 编码实现

https://github.com/MarsonShine/AlgorithmsLearningStudy/blob/master/csharp/Trees/MerkleTree.cs