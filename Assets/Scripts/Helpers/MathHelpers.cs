using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Extensions
{
    public static class MathHelpers
    {
        /// <summary>
        /// Converts an angle between [0,360] to [-180,180]
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static float RelativeAngle(float angle)
        {
            return Mathf.Abs(angle) > 180 ? angle - 360 : angle;
        }
    }
}
