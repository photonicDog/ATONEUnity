using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Extensions
{
    public static class UnityVectorExtensions
    {
        /// <summary>
        /// Kills the y-component of a Unity Vector3
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static Vector3 KillY(this Vector3 vec)
        {
            vec.Scale(new Vector3(1, 0, 1));
            return vec;
        }
    }
}
