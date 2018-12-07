using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageUtils
{
    public class FilterData
    {
        private readonly double[] _coefficient;
        private readonly int _hSamples;
        private readonly int _vSamples;

        private double _total;

        public FilterData(double[] coefficient, int hsamples, int vsamples)
        {
            if (coefficient.Length != (hsamples * vsamples)) {
                throw new ArgumentException("Coefficient arrays not match. coefficient.Len="
                    + coefficient.Length + ", hsample=" + hsamples + ", vsample=" + vsamples);
            }

            _coefficient = coefficient;
            _hSamples = hsamples;
            _vSamples = vsamples;
            _total = _coefficient.Sum();
        }

        public double[] Coefficient {
            get { return _coefficient; }
        }
        public int HSampleCount {
            get { return _hSamples; }
        }

        public int VSampleCount {
            get { return _vSamples; }
        }

        public double Total {
            get { return _total; }
        }
    }
}
