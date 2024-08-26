using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UAManagedCore;

namespace OEM_Template_V1
{
    public static class OptixExtensions
    {
        public static T GetVariableValue<T>(this IUANode node,string browseName,T defaultValue)
        {
            if(node == null)
            {
                return defaultValue;
            }
            else
            {
                var v = node.GetVariable(browseName);
                if(v == null)
                {
                    return defaultValue;
                }else
                {
                    return (T)v.Value.Value;
                }
            }
        }

        public static bool SetVariableValue<T>(this IUANode node, string browseName, T value)
        {
            if (node == null)
            {
                return false;
            }
            else
            {
                var v = node.GetVariable(browseName);
                if (v == null)
                {
                    return false ;
                }
                else
                {
                    v.Value = new UAValue(value);
                    return true ;
                }
            }
        }

        public static void ClearAll(this IUANode node)
        {
            foreach(var item in node.Children)
            {
                item.Delete();
            }
        }
    }

}
