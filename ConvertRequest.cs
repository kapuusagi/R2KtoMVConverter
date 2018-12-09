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
        private string _outputDirectory;
        public ConvertRequest(String[] files, string outputDirectory)
        {
            _files = files;
            _outputDirectory = outputDirectory;
        }

        public string OutputDirectory {
            get {
                return _outputDirectory;
            }
        }

        public String[] Files {
            get {
                return _files;
            }
        }
    }
}
