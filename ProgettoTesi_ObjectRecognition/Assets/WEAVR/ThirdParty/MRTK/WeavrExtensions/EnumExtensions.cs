#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
namespace TXT.WEAVR.Common
{
    using System;

    public static class EnumExtensions
    {
        public static bool ContainsFlag(this Enum enumValue, Enum flag)
        {
            try
            {
                return enumValue.GetType() == flag.GetType() && ((int)(object)enumValue & (int)(object)flag) != 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
#endif
