using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace R2KtoMVConverter
{
    public class ConvertRequest
    {
        private string[] _files;
        public ConvertRequest(String[] files)
        {
            _files = files;
        }

        public String[] Files {
            get {
                return _files;
            }
        }
    }
}
