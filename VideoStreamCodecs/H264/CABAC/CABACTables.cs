using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  struct MN { public short m; public short n; }
  static class CABACTables
  {
    static MN zero = new MN();
    public static MN[,] T9 = new MN[,] { 

      // Table 9 - 12, ctxIdx = 0 to 10

        {new MN() { m =   20, n =  -15}, zero, zero}
      , {new MN() { m =    2, n =   54}, zero, zero}
      , {new MN() { m =    3, n =   74}, zero, zero}
      , {new MN() { m =   20, n =  -15}, zero, zero}
      , {new MN() { m =    2, n =   54}, zero, zero}
      , {new MN() { m =    3, n =   74}, zero, zero}
      , {new MN() { m =  -28, n =  127}, zero, zero}
      , {new MN() { m =  -23, n =  104}, zero, zero}
      , {new MN() { m =   -6, n =   53}, zero, zero}
      , {new MN() { m =   -1, n =   54}, zero, zero}
      , {new MN() { m =    7, n =   51}, zero, zero}

      // Table 9- 13, ctxIdx = 11 to 23

      , {new MN() { m =   23, n =  33}, new MN() { m =   22, n =  25}, new MN() { m =   29, n =  16}}
      , {new MN() { m =   23, n =   2}, new MN() { m =   34, n =   0}, new MN() { m =   25, n =   0}}
      , {new MN() { m =   21, n =   0}, new MN() { m =   16, n =   0}, new MN() { m =   14, n =   0}}
      , {new MN() { m =    1, n =   9}, new MN() { m =   -2, n =   9}, new MN() { m =  -10, n =  51}}
      , {new MN() { m =    0, n =  49}, new MN() { m =    4, n =  41}, new MN() { m =   -3, n =  62}}
      , {new MN() { m =  -37, n = 118}, new MN() { m =  -29, n = 118}, new MN() { m =  -27, n =  99}}
      , {new MN() { m =    5, n =  57}, new MN() { m =    2, n =  65}, new MN() { m =   26, n =  16}}
      , {new MN() { m =  -13, n =  78}, new MN() { m =   -6, n =  71}, new MN() { m =   -4, n =  85}}
      , {new MN() { m =  -11, n =  65}, new MN() { m =  -13, n =  79}, new MN() { m =  -24, n = 102}}
      , {new MN() { m =    1, n =  62}, new MN() { m =    5, n =  52}, new MN() { m =    5, n =  57}}
      , {new MN() { m =   12, n =  49}, new MN() { m =    9, n =  50}, new MN() { m =    6, n =  57}}
      , {new MN() { m =   -4, n =  73}, new MN() { m =   -3, n =  70}, new MN() { m =  -17, n =  73}}
      , {new MN() { m =   17, n =  50}, new MN() { m =   10, n =  54}, new MN() { m =   14, n =  57}}

      // Table 9 - 14, ctxIdx = 24 to 39

      , {new MN() { m =   18, n =  64}, new MN() { m =   26, n =  34}, new MN() { m =   20, n =  40}}
      , {new MN() { m =    9, n =  43}, new MN() { m =   19, n =  22}, new MN() { m =   20, n =  10}}
      , {new MN() { m =   29, n =   0}, new MN() { m =   40, n =   0}, new MN() { m =   29, n =   0}}
      , {new MN() { m =   26, n =  67}, new MN() { m =   57, n =   2}, new MN() { m =   54, n =   0}}
      , {new MN() { m =   16, n =  90}, new MN() { m =   41, n =  36}, new MN() { m =   37, n =  42}}
      , {new MN() { m =    9, n = 104}, new MN() { m =   26, n =  69}, new MN() { m =   12, n =  97}}
      , {new MN() { m =  -46, n = 127}, new MN() { m =  -45, n = 127}, new MN() { m =  -32, n = 127}}
      , {new MN() { m =  -20, n = 104}, new MN() { m =  -15, n = 101}, new MN() { m =  -22, n = 117}}
      , {new MN() { m =    1, n =  67}, new MN() { m =   -4, n =  76}, new MN() { m =   -2, n =  74}}
      , {new MN() { m =  -13, n =  78}, new MN() { m =   -6, n =  71}, new MN() { m =   -4, n =  85}}
      , {new MN() { m =  -11, n =  65}, new MN() { m =  -13, n =  79}, new MN() { m =  -24, n = 102}}
      , {new MN() { m =    1, n =  62}, new MN() { m =    5, n =  52}, new MN() { m =    5, n =  57}}
      , {new MN() { m =   -6, n =  86}, new MN() { m =    6, n =  69}, new MN() { m =   -6, n =  93}}
      , {new MN() { m =  -17, n =  95}, new MN() { m =  -13, n =  90}, new MN() { m =  -14, n =  88}}
      , {new MN() { m =   -6, n =  61}, new MN() { m =    0, n =  52}, new MN() { m =   -6, n =  44}}
      , {new MN() { m =    9, n =  45}, new MN() { m =    8, n =  43}, new MN() { m =    4, n =  55}}
    };
  }
}
