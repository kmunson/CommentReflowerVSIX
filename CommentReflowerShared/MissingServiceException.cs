using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommentReflower
{
    class MissingServiceException : Exception
    {
        public MissingServiceException(Type ServiceType)
            : base(string.Format("Missing service {0}", ServiceType))
        {
        }
    }
}
