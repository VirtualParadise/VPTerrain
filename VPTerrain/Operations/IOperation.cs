using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VP;

namespace VPTerrain
{
    interface IOperation
    {
        void Run(Instance bot);
        void Dispose();
    }
}
