using Core.Lnk.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Lnk.ExtraData
{
    internal class ExtraDataBlock 
    {
        public ExtraDataBlockSignature Signature { get; set; }
        public string Value { get; set; }
    }
}
