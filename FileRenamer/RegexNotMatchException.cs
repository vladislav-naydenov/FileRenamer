using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileRenamer
{
    public sealed class RegexNotMatchException : Exception
    {
        public RegexNotMatchException(string message)
            : base(message)
        {
        }
    }
}
