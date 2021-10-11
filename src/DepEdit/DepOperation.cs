using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using System.Windows.Forms;

namespace DependencyEdit
{
    enum DOType
    {
        MoveArrow,
        Split,
        Merge
    }

    class DepOperation
    {
        public DepOperation(DOType t, object[] paramlist)
        {
            m_OpType = t;
            m_ParamList = paramlist;
        }

        private DOType m_OpType;
        private object[] m_ParamList;

        public void Execute(Sentence sen)
        {
            try
            {
                switch (m_OpType)
                {
                    case DOType.MoveArrow:
                        {
                            int ibmax = sen.Bunsetsus.Count;
                            int ib = (int)m_ParamList[0];
                            int id_org = (int)m_ParamList[1];
                            int id_new = (int)m_ParamList[2];
                            if (ib < 0 || ib >= ibmax || id_org < 0 || id_org >= ibmax || id_new < 0 || id_new >= ibmax)
                            {
                                throw new IndexOutOfRangeException("Invalid dependency index");
                            }
                            Bunsetsu b = (Bunsetsu)sen.Bunsetsus[ib];
                            b.DependsTo = (Bunsetsu)sen.Bunsetsus[id_new];
                        }
                        break;
                    case DOType.Split:
                        {
                            int b_at = (int)m_ParamList[0];
                            int w_at = (int)m_ParamList[1];
                            sen.SplitBunsetsu(b_at, w_at);
                        }
                        break;
                    case DOType.Merge:
                        {
                            int b_at = (int)m_ParamList[0];
                            //int w_at = (int)m_ParamList[1]; //w_atは逆操作(Split)を行うためにのみ、記録される
                            sen.MergeBunsetsu(b_at);
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Cannot execute Operation(" + this + "): "+ e);
            }
        }

        public void UnExecute(Sentence sen)
        {
            try
            {
                switch (m_OpType)
                {
                    case DOType.MoveArrow:
                        {
                            int ibmax = sen.Bunsetsus.Count;
                            int ib = (int)m_ParamList[0];
                            int id_org = (int)m_ParamList[1];
                            int id_new = (int)m_ParamList[2];
                            if (ib < 0 || ib >= ibmax || id_org < 0 || id_org >= ibmax || id_new < 0 || id_new >= ibmax)
                            {
                                throw new IndexOutOfRangeException("Invalid dependency index");
                            }
                            Bunsetsu b = (Bunsetsu)sen.Bunsetsus[ib];
                            b.DependsTo = (Bunsetsu)sen.Bunsetsus[id_org]; 
                        }
                        break;
                    case DOType.Split:
                        {
                            int b_at = (int)m_ParamList[0];
                            //int w_at = (int)m_ParamList[1]; // 逆操作のMergeでは不要なパラメータ
                            sen.MergeBunsetsu(b_at);
                        }
                        break;
                    case DOType.Merge:
                        {
                            int b_at = (int)m_ParamList[0];
                            int w_at = (int)m_ParamList[1];
                            sen.SplitBunsetsu(b_at, w_at);
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Cannot un-execute Operation: " + this);
            }
        }

        public override string ToString()
        {
            string s = "{" + this.m_OpType + ":";
            foreach (object o in m_ParamList)
            {
                s += o;
                s += ", ";
            }
            s += "}";
            return s;
        }
    }
}
