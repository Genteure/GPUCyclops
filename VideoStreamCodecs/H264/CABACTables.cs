using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  struct MandN { public short m; public short n; }
  class CABACTables
  {
    MandN[] T9_12 = new MandN[10] { new MandN() { m =   20, n =  -15 },
                                    new MandN() { m =    2, n =   54 },
                                    new MandN() { m =    3, n =   74 },
                                    new MandN() { m =   20, n =  -15 },
                                    new MandN() { m =    2, n =   54 },
                                    new MandN() { m =    3, n =   74 },
                                    new MandN() { m =  -28, n =  127 },
                                    new MandN() { m =  -23, n =  104 },
                                    new MandN() { m =   -1, n =   54 },
                                    new MandN() { m =    7, n =   51 }
                    };
  }
}
