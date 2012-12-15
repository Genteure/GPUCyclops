using System.Reflection;

namespace Media.Formats.MP4
{
    using System;
    using System.Globalization;

    public static class EnumUtils
    {
        public static bool IsBitSet<T>(T value, T check) where T: IConvertible
        {
            int num = value.ToInt32(CultureInfo.InvariantCulture);
            int num2 = check.ToInt32(CultureInfo.InvariantCulture);
            return ((num & num2) == num2);
        }

        public static void SetBit<T>(ref uint value, T inBit) where T : IConvertible
        {
          value |= inBit.ToUInt32(CultureInfo.InvariantCulture);
        }

        public static void ResetBit<T>(ref uint value, T inBit) where T : IConvertible
        {
          value &= ~inBit.ToUInt32(CultureInfo.InvariantCulture); 
        }

        public static bool IsValid<T>(T value)
        {
            return Enum.IsDefined(typeof(T), value);
        }

        public static Array GetValues(this Enum enumType) {
          Type type = enumType.GetType();

          FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

          Array array = Array.CreateInstance(type, fields.Length);

          for (int i = 0; i < fields.Length; i++) {
            var obj = fields[i].GetValue(null);
            array.SetValue(obj, i);
          }

          return array;
        }

        public static Array GetValues(this Type enumType) {
          Type type = enumType.GetType();

          FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

          Array array = Array.CreateInstance(type, fields.Length);

          for (int i = 0; i < fields.Length; i++) {
            var obj = fields[i].GetValue(null);
            array.SetValue(obj, i);
          }

          return array;
        }

    }
}
