using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSL.Trees
{
    public class NTree<T>
    {
        private T m_Data;
        private LinkedList<NTree<T>> m_Children;

        public NTree(T data)
        {
            this.m_Data = data;
            m_Children = new LinkedList<NTree<T>>();
        }


        public T GetData()
        {
            return m_Data;
        }

        public void SetData(T data)
        {
            this.m_Data = data;
        }

        public void AddChild(T data)
        {
            m_Children.AddLast(new NTree<T>(data));
        }

        public void AddChild(NTree<T> tree)
        {
            m_Children.AddLast(tree);
        }

        public bool IsLeaf()
        {
            if (m_Children.Count == 0) return true;
            return false;
        }

        public NTree<T>? GetChild(int i)
        {
            return m_Children.ElementAt(i);
        }

        public LinkedList<NTree<T>> GetChilds()
        {
            return m_Children;
        }

        public void PreorderTraverse(Action<NTree<T>> func)
        {
            DeepPreorderTraverse(this, func);
        }

        public void PostorderTraverse(Action<NTree<T>> func)
        {
            DeepPostorderTraverse(this, func);
        }

        private static void DeepPreorderTraverse(NTree<T> node, Action<NTree<T>> visitor)
        {
            visitor(node);
            foreach (NTree<T> child in node.m_Children)
                DeepPreorderTraverse(child, visitor);
        }
        private static void DeepPostorderTraverse(NTree<T> node, Action<NTree<T>> visitor)
        {
            foreach (NTree<T> child in node.m_Children)
                DeepPreorderTraverse(child, visitor);
            visitor(node);
        }
    }
}
