using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Common
{
    public interface INestable
    {
        int Level { get; set; }
    }

    // TolopogicalSortの要因となる半順序 ( true if, x is parent of y )
    public delegate bool IsParent<T>(T x, T y);

    // DAGの要素
    class DAGNode<T> where T: class
    {
        public T Node;
        public List<DAGNode<T>> Parents;

        public DAGNode()
        {
            this.Node = null;
            this.Parents = new List<DAGNode<T>>();
        }
        public DAGNode(T t)
            : this()
        {
            this.Node = t;
        }
    }

    class DAGEdge<T> : IEquatable<DAGEdge<T>> where T: class
    {
        public DAGNode<T> From;
        public DAGNode<T> To;

        public DAGEdge(DAGNode<T> from, DAGNode<T> to)
        {
            this.From = from;
            this.To = to;
        }

        public bool Equals(DAGEdge<T> other)
        {
            return (this.From == other.From && this.To == other.To);
        }
    }

    /// <summary>
    /// 親子の半順序がある要素リストをトポロジカルソートする.
    /// 結果は要素のLevel値に反映され、末端のLevelが0,
    /// 親は全ての子のLevelよりも大きな値を持つようにする.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TopologicalSort<T> where T : class, INestable
    {
        private List<T> m_Source;
        // 末端から逆に親方向へ辿るツリーの末端を束ねるルートノード
        private DAGNode<T> m_DAGRoot;

        public TopologicalSort(List<T> input, IsParent<T> comparer)
        {
            m_Source = input;
            m_DAGRoot = new DAGNode<T>();
            foreach (T t in input)
            {
                m_DAGRoot.Parents.Add(new DAGNode<T>(t));
            }
            var tmp = new List<DAGNode<T>>(m_DAGRoot.Parents);
            foreach (DAGNode<T> n1 in m_DAGRoot.Parents)
            {
                foreach (DAGNode<T> n2 in m_DAGRoot.Parents)
                {
                    if (n1 == n2) continue;
                    if (comparer(n1.Node, n2.Node))
                    {
                        // n1 is parent of n2, so...
                        n2.Parents.Add(n1);
                        tmp.Remove(n1);
                    }
                }
            }
            m_DAGRoot.Parents = tmp;
        }

        public void Sort()
        {
            VisitNode(m_DAGRoot, 0, new List<DAGEdge<T>>());

        }

        private void VisitNode(DAGNode<T> node, int curDepth, List<DAGEdge<T>> visited_edges)
        {
            if (node.Node != null)
            {
                curDepth = node.Node.Level = Math.Max(node.Node.Level, curDepth);
            }

            if (node.Parents.Count > 0)
            {
                curDepth++;
                foreach (DAGNode<T> t in node.Parents)
                {
                    var v = new DAGEdge<T>(node, t);
                    if (visited_edges.Contains(v))
                    {
                        Console.WriteLine("Cyclic link found in TopologicalSort.VisitEdge; ignored!");
                        continue;
                    }
                    visited_edges.Add(v);
                    VisitNode(t, curDepth, visited_edges);
                }
            }
            //visited_list.Remove(node); // ツリー状とは限らないので、クリアしない
        }
    }
}
