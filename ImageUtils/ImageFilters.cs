using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageUtils
{
    public class ImageFilters
    {
        private static readonly double[] M3x3Coefficient = {
            0, 1, 0,
            1, 4, 1,
            0, 1, 0
        };

        public static readonly FilterData M3x3Filter
            = new FilterData(M3x3Coefficient, 3, 3);
    }
}
